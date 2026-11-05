using System.Text.Json;
using System.Threading.Channels;
using Comments.Api.Application.Abstractions;
using Comments.Api.Application.Options;
using Comments.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Comments.Api.Infrastructure.Messaging;

// Запрос только кладёт событие в очередь в памяти (это мгновенно), а отправляет его фоновая задача.
// Поэтому недоступный RabbitMQ не замедляет и не ломает создание комментария.
// Если связь потеряна, задача переподключается. Что не успело уйти, может потеряться, и это допустимо.
// [разбор] Это зеркало приёма в Comments.Realtime: там слушают, здесь отправляют. Устройство такое же (BackgroundService и цикл переподключения).
// [разбор] Главное отличие: между запросом и RabbitMQ стоит очередь в памяти (queue). Запрос кладёт туда событие и сразу возвращается,
// [разбор] а фоновая задача по одному отправляет их в брокер. Поэтому медленный или упавший RabbitMQ не тормозит создание комментария.
public sealed class RabbitMqCommentEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqCommentEventPublisher> logger) : BackgroundService, ICommentEventPublisher
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions settings = options.Value;

    // [разбор] Та же ограниченная очередь, что и CommentEventBuffer в Realtime: при переполнении выбрасываются самые старые события.
    private readonly Channel<CommentCreatedEvent> queue = Channel.CreateBounded<CommentCreatedEvent>(
        new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    // [разбор] Это вызывает CommentService после сохранения комментария. Событие просто попадает в очередь в памяти, и всё.
    public void Publish(CommentCreatedEvent @event) => queue.Writer.TryWrite(@event);

    // [разбор] Цикл: подключиться и отправлять; если связь пропала, подождать 5 секунд и подключиться заново (как в приёме).
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishUntilConnectionLostAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Нет связи с RabbitMQ: {Message}. Повтор через {Seconds} с.", exception.Message, RetryDelay.TotalSeconds);
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }

    private async Task PublishUntilConnectionLostAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            ClientProvidedName = "comments-api",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            // Переподключением управляет цикл выше: так проще понять, что происходит.
            AutomaticRecoveryEnabled = false
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // [разбор] Объявляем exchange. Если его ещё нет, он создастся. Если Realtime уже создал такой же, ничего не изменится.
        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.CommentCreatedExchange,
            ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        // Обрыв связи останавливает чтение очереди, и цикл выше переподключается.
        using var connectionLost = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            connectionLost.Cancel();
            return Task.CompletedTask;
        };

        logger.LogInformation("Подключено к RabbitMQ ({Host}:{Port}), exchange {Exchange}.", settings.Host, settings.Port, RabbitMqTopology.CommentCreatedExchange);

        // [разбор] ReadAllAsync ждёт появления событий в очереди и отдаёт их по одному. Пока очередь пуста, цикл просто спит.
        await foreach (var @event in queue.Reader.ReadAllAsync(connectionLost.Token))
        {
            // [разбор] Событие превращается в JSON (в байты). Получатель разбирает его обратно в тот же CommentCreatedEvent.
            var body = JsonSerializer.SerializeToUtf8Bytes(@event);

            // [разбор] Отправляем в exchange. routingKey пустой, потому что fanout раздаёт всем очередям без разбора по ключу.
            await channel.BasicPublishAsync(
                RabbitMqTopology.CommentCreatedExchange,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body,
                cancellationToken: connectionLost.Token);
        }
    }
}

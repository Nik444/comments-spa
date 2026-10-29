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
public sealed class RabbitMqCommentEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqCommentEventPublisher> logger) : BackgroundService, ICommentEventPublisher
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions settings = options.Value;

    private readonly Channel<CommentCreatedEvent> queue = Channel.CreateBounded<CommentCreatedEvent>(
        new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Publish(CommentCreatedEvent @event) => queue.Writer.TryWrite(@event);

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

        await foreach (var @event in queue.Reader.ReadAllAsync(connectionLost.Token))
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(@event);

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

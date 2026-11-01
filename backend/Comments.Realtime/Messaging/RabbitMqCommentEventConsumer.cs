using System.Text.Json;
using Comments.Contracts;
using Comments.Realtime.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Comments.Realtime.Messaging;

// У каждого экземпляра сервиса своя временная очередь, привязанная к fanout-exchange: события получают все экземпляры.
// Очередь исчезает вместе с подключением. Пока сервис не работает, события для него не копятся, и это допустимо:
// после перезапуска клиенты всё равно переподключаются и перезагружают данные.
// [разбор] BackgroundService это класс, который запускается вместе с сервисом и работает в фоне, пока сервис жив.
// [разбор] Здесь он подключается к RabbitMQ, слушает события и кладёт их в буфер (CommentEventBuffer).
// [разбор] Схема RabbitMQ: издатель (API) кладёт сообщение в exchange. Exchange раздаёт копии во все очереди, привязанные к нему.
// [разбор] Тип fanout значит "раздать всем". Из очереди сообщение забирает consumer (это мы).
public sealed class RabbitMqCommentEventConsumer(
    IOptions<RabbitMqOptions> options,
    CommentEventBuffer buffer,
    ILogger<RabbitMqCommentEventConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions settings = options.Value;

    // [разбор] Точка входа задачи. Идея: подключиться и слушать; если связь пропала, подождать 5 секунд и начать заново.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // [разбор] stoppingToken срабатывает, когда сервис останавливают. Тогда цикл заканчивается.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilConnectionLostAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // [разбор] Это нормальная остановка сервиса, а не ошибка.
                return;
            }
            catch (Exception exception)
            {
                // [разбор] Любая другая причина (брокер не запущен, пропала сеть): пишем предупреждение и пробуем снова.
                logger.LogWarning("Нет связи с RabbitMQ: {Message}. Повтор через {Seconds} с.", exception.Message, RetryDelay.TotalSeconds);
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }

    private async Task ConsumeUntilConnectionLostAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            // [разбор] Это имя видно в веб-интерфейсе RabbitMQ на вкладке Connections: удобно понять, кто подключён.
            ClientProvidedName = "comments-realtime",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            // [разбор] Свой цикл переподключения (выше) проще понять, чем встроенное автовосстановление, поэтому его выключаем.
            AutomaticRecoveryEnabled = false
        };

        // [разбор] Connection это TCP-подключение к брокеру. Channel это "линия" внутри него, по которой идут команды.
        // [разбор] await using: при выходе из метода оба закроются сами.
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // [разбор] Создаём exchange, если его ещё нет (если уже есть с теми же параметрами, ничего не меняется).
        // [разбор] Так порядок запуска не важен: неважно, что стартовало раньше, API или этот сервис.
        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.CommentCreatedExchange,
            ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        // [разбор] Своя очередь этого экземпляра. Пустое имя значит "придумай сам" (получится amq.gen-...).
        // [разбор] exclusive: очередью пользуется только это подключение. autoDelete: когда подключение закрылось, очередь удалится.
        // [разбор] durable: false: очередь не переживёт перезапуск брокера, и это нам подходит.
        var queue = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: stoppingToken);

        // [разбор] Привязываем очередь к exchange: теперь копия каждого события попадёт и в неё.
        await channel.QueueBindAsync(queue.QueueName, RabbitMqTopology.CommentCreatedExchange, routingKey: string.Empty, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        // [разбор] Этот код вызывается на каждое сообщение, которое пришло в очередь.
        consumer.ReceivedAsync += (_, delivery) =>
        {
            Receive(delivery.Body);
            return Task.CompletedTask;
        };

        // Подтверждения не нужны: потерянный сигнал не страшен, зато приём не замедляется.
        // [разбор] autoAck: брокер считает сообщение доставленным сразу при отправке нам, не ждёт подтверждения.
        // [разбор] Если бы мы падали посреди обработки, сообщение пропало бы, но для сигналов это допустимо.
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer, stoppingToken);

        // [разбор] linked token срабатывает в двух случаях: сервис останавливают, или оборвалась связь с брокером.
        using var connectionLost = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            connectionLost.Cancel();
            return Task.CompletedTask;
        };

        logger.LogInformation("Подключено к RabbitMQ ({Host}:{Port}), очередь {Queue}.", settings.Host, settings.Port, queue.QueueName);

        // Дальше всё делает consumer, а здесь ждём обрыва связи или остановки сервиса.
        // [разбор] Delay с бесконечным временем это просто "спать, пока не отменят". Отмена выбросит исключение,
        // [разбор] метод завершится, а цикл в ExecuteAsync переподключится заново.
        await Task.Delay(Timeout.Infinite, connectionLost.Token);
    }

    // [разбор] Превращает полученные байты обратно в событие и кладёт его в буфер.
    private void Receive(ReadOnlyMemory<byte> body)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<CommentCreatedEvent>(body.Span);

            if (@event is not null)
                buffer.Add(@event);
        }
        catch (JsonException exception)
        {
            // [разбор] Одно битое сообщение не должно ронять приём: пишем в лог и идём дальше.
            logger.LogWarning("Получено непонятное сообщение, пропускаю: {Message}", exception.Message);
        }
    }
}

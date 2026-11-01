using Comments.Realtime.Hubs;
using Comments.Realtime.Messaging;
using Comments.Realtime.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Comments.Realtime.Broadcasting;

// Раз в интервал забирает накопленные события и отправляет клиентам одну пачку сигналов.
// Так при потоке комментариев клиент получает не больше одного сообщения за интервал, а не по сообщению на комментарий.
// [разбор] IHubContext позволяет отправлять сообщения клиентам не изнутри хаба, а из любого другого места, например из фоновой задачи.
public sealed class CommentSignalBroadcaster(
    CommentEventBuffer buffer,
    IHubContext<CommentsHub, ICommentsClient> hub,
    IOptions<RealtimeOptions> options,
    ILogger<CommentSignalBroadcaster> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // [разбор] PeriodicTimer "тикает" с заданным интервалом (по умолчанию 1 секунда). Каждый тик запускает одну рассылку.
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.BatchIntervalMilliseconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await BroadcastAsync();
        }
        catch (OperationCanceledException)
        {
            // [разбор] Сервис останавливают, это не ошибка.
        }
    }

    private async Task BroadcastAsync()
    {
        // [разбор] Забираем всё, что накопилось с прошлого тика, и превращаем в пачку.
        var batch = SignalBatch.From(buffer.TakeAll());

        if (batch.IsEmpty)
            return;

        try
        {
            // [разбор] Clients.All: всем подключённым. Новые корни интересуют всех независимо от того, какую страницу они смотрят.
            if (batch.NewRootIds.Count > 0)
                await hub.Clients.All.NewRoots(batch.NewRootIds);

            // [разбор] Для каждой ветки с новыми ответами шлём сигнал только в группу этой ветки:
            // [разбор] его получат лишь те, кто подписался на неё через SubscribeRoots. WhenAll отправляет их одновременно.
            await Task.WhenAll(batch.NewReplyIdsByRoot.Select(pair =>
                hub.Clients.Group(CommentsHub.GroupName(pair.Key)).NewReplies(pair.Key, pair.Value)));

            logger.LogDebug("Отправлено сигналов: корней {Roots}, веток с ответами {Threads}.", batch.NewRootIds.Count, batch.NewReplyIdsByRoot.Count);
        }
        catch (Exception exception)
        {
            // [разбор] Сбой отправки не должен убить задачу: запишем в лог, и следующий тик попробует снова с новыми событиями.
            logger.LogWarning(exception, "Не удалось отправить пачку сигналов.");
        }
    }
}

using Comments.Realtime.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Comments.Realtime.Hubs;

// [разбор] Hub в SignalR похож на контроллер в API, только подключение не разовое, а живое (WebSocket).
// [разбор] Hub<ICommentsClient> значит: методы этого класса вызывает браузер, а у браузера мы вызываем методы ICommentsClient.
// [разбор] Экземпляр хаба создаётся заново на каждый вызов метода, состояния в полях хранить нельзя.
public sealed class CommentsHub(IOptions<RealtimeOptions> options) : Hub<ICommentsClient>
{
    // [разбор] Ключ, под которым в Context.Items лежит набор веток, на которые уже подписано это подключение.
    private const string SubscribedRootsKey = "roots";

    // [разбор] Группа в SignalR это просто имя, к которому подключения можно добавлять и потом слать сообщения всем сразу.
    // [разбор] Для каждой ветки своя группа: "root:15" для корневого комментария с Id 15.
    public static string GroupName(long rootId) => $"root:{rootId}";

    // Страница сообщает, какие корневые комментарии на ней показаны. Повторный вызов заменяет прежний набор.
    // При обрыве связи SignalR сам убирает подключение из групп, поэтому отписываться не нужно.
    // [разбор] Этот метод вызывает браузер: connection.invoke('SubscribeRoots', [1, 2, 3]).
    public async Task SubscribeRoots(long[] rootIds)
    {
        var max = options.Value.MaxRootsPerSubscription;

        // [разбор] Защита: нельзя подписаться на тысячи веток сразу. HubException доходит до браузера как понятная ошибка.
        if (rootIds.Length > max)
            throw new HubException($"Можно подписаться не более чем на {max} комментариев.");

        // [разбор] wanted: на что хочет быть подписан браузер сейчас. current: на что подписан уже.
        var wanted = rootIds.ToHashSet();
        var current = Context.Items.TryGetValue(SubscribedRootsKey, out var value) ? (HashSet<long>)value! : [];

        // [разбор] Что было, но больше не нужно (пользователь перешёл на другую страницу): убираем из групп.
        foreach (var rootId in current.Except(wanted))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(rootId));

        // [разбор] Что нужно, но ещё не было: добавляем в группы. Те, что остались без изменений, не трогаем.
        foreach (var rootId in wanted.Except(current))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(rootId));

        // [разбор] Запоминаем новый набор для этого подключения, чтобы при следующем вызове знать, что менять.
        Context.Items[SubscribedRootsKey] = wanted;
    }
}

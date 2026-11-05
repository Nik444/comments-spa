using Comments.Contracts;

namespace Comments.Realtime.Broadcasting;

/// <summary>Пачка сигналов за один тик: новые корни и новые ответы по веткам.</summary>
// [разбор] Это просто данные, без работы с сетью: поэтому её легко проверить обычными тестами (SignalBatchTests).
// [разбор] NewRootIds: Id новых корневых комментариев. NewReplyIdsByRoot: для каждой ветки список Id новых ответов.
public sealed record SignalBatch(IReadOnlyList<long> NewRootIds, IReadOnlyDictionary<long, long[]> NewReplyIdsByRoot)
{
    // [разбор] Пустая пачка значит "ничего нового за этот тик", слать клиентам нечего.
    public bool IsEmpty => NewRootIds.Count == 0 && NewReplyIdsByRoot.Count == 0;

    // [разбор] Разбирает сырой список событий на две части: корневые комментарии отдельно, ответы по веткам отдельно.
    public static SignalBatch From(IEnumerable<CommentCreatedEvent> events)
    {
        var roots = new List<long>();
        var replies = new Dictionary<long, List<long>>();

        foreach (var @event in events)
        {
            // [разбор] У корневого комментария RootId пустой. Его Id идёт в список новых корней.
            if (@event.RootId is not { } rootId)
            {
                roots.Add(@event.Id);
            }
            // [разбор] Иначе это ответ. Если для этой ветки список уже есть, дописываем в него.
            else if (replies.TryGetValue(rootId, out var ids))
            {
                ids.Add(@event.Id);
            }
            // [разбор] Иначе заводим для ветки новый список с этим ответом.
            else
            {
                replies[rootId] = [@event.Id];
            }
        }

        // [разбор] Id сортируем по возрастанию: родитель всегда создан раньше ребёнка, клиенту так удобнее вставлять в дерево.
        roots.Sort();

        return new SignalBatch(
            roots,
            replies.ToDictionary(pair => pair.Key, pair => pair.Value.Order().ToArray()));
    }
}

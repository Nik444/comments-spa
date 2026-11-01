using System.Threading.Channels;
using Comments.Contracts;
using Comments.Realtime.Options;
using Microsoft.Extensions.Options;

namespace Comments.Realtime.Messaging;

// Между приёмом из RabbitMQ и рассылкой клиентам: события копятся здесь, пока их не заберёт очередная пачка.
// [разбор] Зачем он нужен: события приходят из RabbitMQ по одному и в любой момент, а клиентам мы шлём раз в секунду.
// [разбор] Буфер это просто очередь в памяти: один кладёт события, другой раз в секунду забирает всё накопленное.
public sealed class CommentEventBuffer(IOptions<RealtimeOptions> options)
{
    // [разбор] Channel это готовая потокобезопасная очередь из .NET: можно писать и читать из разных потоков без блокировок.
    // [разбор] Bounded: у очереди есть предел размера. DropOldest: если она переполнилась, самое старое событие выбрасывается.
    // [разбор] Это защита памяти: если рассылка не успевает, мы теряем старые сигналы, а не растём бесконечно.
    // [разбор] SingleReader: читает только одна задача (рассылка), это ускоряет работу очереди.
    private readonly Channel<CommentCreatedEvent> events = Channel.CreateBounded<CommentCreatedEvent>(
        new BoundedChannelOptions(options.Value.BufferCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    // [разбор] Вызывается из приёма RabbitMQ на каждое событие. TryWrite не ждёт и не блокирует.
    public void Add(CommentCreatedEvent @event) => events.Writer.TryWrite(@event);

    // [разбор] Вызывается рассылкой раз в секунду: забирает всё, что накопилось, и очищает очередь.
    public IReadOnlyList<CommentCreatedEvent> TakeAll()
    {
        var taken = new List<CommentCreatedEvent>();

        // [разбор] TryRead возвращает false, когда очередь пуста, тогда цикл заканчивается.
        while (events.Reader.TryRead(out var @event))
            taken.Add(@event);

        return taken;
    }
}

using Comments.Contracts;
using Comments.Realtime.Broadcasting;

namespace Comments.Tests.Realtime;

public class SignalBatchTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    private static CommentCreatedEvent Root(long id) => new(id, null, null, Now);

    private static CommentCreatedEvent Reply(long id, long parentId, long rootId) => new(id, parentId, rootId, Now);

    [Fact]
    public void From_ReturnsEmptyBatch_ForNoEvents()
    {
        Assert.True(SignalBatch.From([]).IsEmpty);
    }

    [Fact]
    public void From_CollectsRootsInAscendingOrder()
    {
        var batch = SignalBatch.From([Root(30), Root(10), Root(20)]);

        Assert.Equal(new long[] { 10, 20, 30 }, batch.NewRootIds);
        Assert.Empty(batch.NewReplyIdsByRoot);
    }

    [Fact]
    public void From_GroupsRepliesByRoot()
    {
        var batch = SignalBatch.From([Reply(13, 10, 10), Reply(12, 10, 10), Reply(21, 20, 20)]);

        Assert.Empty(batch.NewRootIds);
        Assert.Equal(new long[] { 12, 13 }, batch.NewReplyIdsByRoot[10]);
        Assert.Equal(new long[] { 21 }, batch.NewReplyIdsByRoot[20]);
    }

    [Fact]
    public void From_SeparatesRootsFromReplies()
    {
        var batch = SignalBatch.From([Root(5), Reply(6, 5, 5)]);

        Assert.Equal(new long[] { 5 }, batch.NewRootIds);
        Assert.Equal(new long[] { 6 }, batch.NewReplyIdsByRoot[5]);
        Assert.False(batch.IsEmpty);
    }
}

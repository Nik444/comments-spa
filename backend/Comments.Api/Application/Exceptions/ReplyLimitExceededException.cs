namespace Comments.Api.Application.Exceptions;

public sealed class ReplyLimitExceededException(long rootId, int limit) : Exception($"В ветке {rootId} достигнут предел {limit} ответов.")
{
    public long RootId { get; } = rootId;

    public int Limit { get; } = limit;
}

namespace Comments.Api.Application.Exceptions;

public sealed class TooManyIdsException(int limit) : Exception($"В одном запросе можно указать не больше {limit} Id.")
{
    public int Limit { get; } = limit;
}

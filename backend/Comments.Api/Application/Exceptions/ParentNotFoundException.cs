namespace Comments.Api.Application.Exceptions;

public sealed class ParentNotFoundException(long parentId) : Exception($"Комментарий {parentId} не найден.")
{
    public long ParentId { get; } = parentId;
}

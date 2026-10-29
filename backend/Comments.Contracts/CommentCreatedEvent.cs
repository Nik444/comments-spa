namespace Comments.Contracts;

/// <summary>
/// Событие «добавлен комментарий». Только идентификаторы: текст и вложения по сети не ходят.
/// У корневого комментария ParentId и RootId равны null.
/// </summary>
public sealed record CommentCreatedEvent(long Id, long? ParentId, long? RootId, DateTime CreatedAt);

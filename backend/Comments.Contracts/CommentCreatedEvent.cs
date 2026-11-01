namespace Comments.Contracts;

/// <summary>
/// Событие «добавлен комментарий». Только идентификаторы: текст и вложения по сети не ходят.
/// У корневого комментария ParentId и RootId равны null.
/// </summary>
// [разбор] record это класс для данных: он сам умеет сравнивать значения и красиво выводиться. Здесь это "письмо" между сервисами.
// [разбор] Сериализуется в JSON: ключи в JSON совпадают с именами свойств (Id, ParentId, RootId, CreatedAt).
public sealed record CommentCreatedEvent(long Id, long? ParentId, long? RootId, DateTime CreatedAt);

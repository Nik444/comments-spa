using Comments.Contracts;

namespace Comments.Api.Application.Abstractions;

public interface ICommentEventPublisher
{
    // Не ждёт брокер и не бросает ошибок: комментарий уже сохранён, потеря сигнала не страшна.
    void Publish(CommentCreatedEvent @event);
}

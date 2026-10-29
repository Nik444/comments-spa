using Comments.Api.Application.Abstractions;
using Comments.Contracts;

namespace Comments.Api.Infrastructure.Messaging;

public sealed class NullCommentEventPublisher : ICommentEventPublisher
{
    public void Publish(CommentCreatedEvent @event)
    {
    }
}

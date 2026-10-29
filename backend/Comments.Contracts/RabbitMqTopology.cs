namespace Comments.Contracts;

public static class RabbitMqTopology
{
    /// <summary>Exchange типа fanout: каждое событие получают все подключённые экземпляры Comments.Realtime.</summary>
    public const string CommentCreatedExchange = "comments.created";
}

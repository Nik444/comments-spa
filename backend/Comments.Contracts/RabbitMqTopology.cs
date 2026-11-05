namespace Comments.Contracts;

public static class RabbitMqTopology
{
    /// <summary>Exchange типа fanout: каждое событие получают все подключённые экземпляры Comments.Realtime.</summary>
    // [разбор] Имя лежит в общей сборке, чтобы издатель и получатель не могли разойтись из-за опечатки в строке.
    public const string CommentCreatedExchange = "comments.created";
}

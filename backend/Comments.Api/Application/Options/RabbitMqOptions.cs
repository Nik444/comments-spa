using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>Публиковать ли события в RabbitMQ. Если выключено, события просто не отправляются.</summary>
    public bool Enabled { get; set; } = true;

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Сколько событий ждёт отправки в памяти, пока RabbitMQ недоступен. При переполнении самые старые отбрасываются.</summary>
    [Range(100, 1_000_000)]
    public int QueueCapacity { get; set; } = 10_000;
}

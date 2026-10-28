using System.ComponentModel.DataAnnotations;

namespace Comments.Api.Application.Options;

public sealed class CaptchaOptions
{
    public const string SectionName = "Captcha";

    /// <summary>Включена ли проверка капчи. Выключать только для нагрузочных тестов и локальной отладки.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Сколько символов в коде на картинке.</summary>
    [Range(4, 8)]
    public int CodeLength { get; set; } = 5;

    /// <summary>Сколько минут код можно ввести после показа картинки.</summary>
    [Range(1, 60)]
    public int LifetimeMinutes { get; set; } = 5;

    /// <summary>Сколько неиспользованных кодов хранится одновременно. Защита от забивания памяти запросами картинок.</summary>
    [Range(100, 10_000_000)]
    public int MaxStoredCodes { get; set; } = 100_000;
}

namespace Comments.Api.Domain;

/// <summary>Предельные размеры полей комментария: единый источник для БД и валидации.</summary>
public static class CommentLimits
{
    public const int UserNameMax = 50;
    public const int EmailMax = 254;        // максимум по RFC
    public const int HomePageMax = 2048;    // максимум URL в большинстве браузеров
    public const int TextMax = 4000;        // предел nvarchar(n) в SQL Server без max

    public const int AttachmentKeyMax = 100;
    public const int AttachmentNameMax = 255;
}

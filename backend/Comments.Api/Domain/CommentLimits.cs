namespace Comments.Api.Domain;

public static class CommentLimits
{
    public const int UserNameMax = 50;
    public const string UserNamePattern = "[A-Za-z0-9]+";   // по ТЗ: цифры и буквы латинского алфавита
    public const int EmailMax = 254;        // максимум по RFC
    public const int HomePageMax = 2048;    // максимум URL в большинстве браузеров
    public const int TextMax = 4000;        // предел nvarchar(n) в SQL Server без max

    public const int AttachmentKeyMax = 100;
    public const int AttachmentNameMax = 255;

    public const int AttachmentImageMaxWidth = 320;
    public const int AttachmentImageMaxHeight = 240;
    public const int AttachmentImageMaxBytes = 1024 * 1024;
    public const int AttachmentTextMaxBytes = 100 * 1024;

    // Файл плюс остальные поля формы. Больше Kestrel не принимает, не дочитывая тело.
    public const int RequestMaxBytes = 2 * 1024 * 1024;
}

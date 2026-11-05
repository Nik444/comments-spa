namespace Comments.Api.Domain;

// Ключ файла: 32 hex-символа от Guid и расширение. По ключу нельзя выйти из папки хранилища и нельзя угадать чужой файл.
public static class AttachmentKeys
{
    private const int IdLength = 32;

    private static readonly string[] Extensions = [".png", ".jpg", ".gif", ".txt"];

    public static string Create(string extension) => Guid.NewGuid().ToString("N") + extension;

    public static bool IsValid(string? key)
    {
        if (key is null || key.Length != IdLength + 4)
            return false;

        for (var i = 0; i < IdLength; i++)
            if (!char.IsAsciiHexDigitLower(key[i]))
                return false;

        return Extensions.Contains(key[IdLength..]);
    }

    public static string ContentType(string key) => Path.GetExtension(key) switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".gif" => "image/gif",
        _ => "text/plain; charset=utf-8"
    };

    public static string Url(string key) => $"/api/attachments/{key}";
}

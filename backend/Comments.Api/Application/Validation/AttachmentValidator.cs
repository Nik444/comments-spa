using System.Text;
using Comments.Api.Domain;

namespace Comments.Api.Application.Validation;

public sealed record AttachmentInspection(AttachmentKind Kind, string Extension, byte[] Data);

// Формат определяется по самому содержимому, а не по расширению и Content-Type, которые присылает клиент и которым верить нельзя.
// Картинку целиком не декодируем: размеры читаются из заголовка, поэтому "бомба распаковки" ничего не стоит серверу.
public static class AttachmentValidator
{
    private const string UnsupportedType = "Допустимы файлы JPG, GIF, PNG и TXT.";

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private enum ImageKind { None, Png, Gif, Jpeg }

    public static string? Validate(string? fileName, Stream content) => Inspect(fileName, content, out _);

    // Проверяет файл и возвращает его содержимое с определённым типом: сохраняется ровно то, что проверено.
    public static string? Inspect(string? fileName, Stream content, out AttachmentInspection? inspection)
    {
        inspection = null;
        var data = ReadLimited(content);

        if (data.Length == 0)
            return "Файл пустой.";

        if (data.Length > CommentLimits.AttachmentImageMaxBytes)
            return $"Файл больше {CommentLimits.AttachmentImageMaxBytes / 1024 / 1024} МБ.";

        var kind = Detect(data);
        var error = kind == ImageKind.None ? ValidateText(fileName, data) : ValidateImage(kind, data);

        if (error is null)
            inspection = new AttachmentInspection(kind == ImageKind.None ? AttachmentKind.Text : AttachmentKind.Image, ExtensionOf(kind), data);

        return error;
    }

    private static string ExtensionOf(ImageKind kind) => kind switch
    {
        ImageKind.Png => ".png",
        ImageKind.Gif => ".gif",
        ImageKind.Jpeg => ".jpg",
        _ => ".txt"
    };

    private static byte[] ReadLimited(Stream content)
    {
        using var memory = new MemoryStream();
        var chunk = new byte[8192];
        int read;

        while ((read = content.Read(chunk)) > 0)
        {
            memory.Write(chunk, 0, read);

            if (memory.Length > CommentLimits.AttachmentImageMaxBytes)
                break;
        }

        return memory.ToArray();
    }

    private static ImageKind Detect(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith(PngSignature))
            return ImageKind.Png;

        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
            return ImageKind.Gif;

        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return ImageKind.Jpeg;

        return ImageKind.None;
    }

    private static string? ValidateImage(ImageKind kind, ReadOnlySpan<byte> data)
    {
        var size = kind switch
        {
            ImageKind.Png => ReadPngSize(data),
            ImageKind.Gif => ReadGifSize(data),
            _ => ReadJpegSize(data)
        };

        if (size is null)
            return "Не удалось определить размеры изображения.";

        var (width, height) = size.Value;

        if (width == 0 || height == 0)
            return "Не удалось определить размеры изображения.";

        if (width > CommentLimits.AttachmentImageMaxWidth || height > CommentLimits.AttachmentImageMaxHeight)
            return $"Изображение не больше {CommentLimits.AttachmentImageMaxWidth}×{CommentLimits.AttachmentImageMaxHeight} пикселей, а у вас {width}×{height}.";

        return null;
    }

    private static (int Width, int Height)? ReadPngSize(ReadOnlySpan<byte> data)
    {
        if (data.Length < 24 || !data[12..16].SequenceEqual("IHDR"u8))
            return null;

        var width = (long)data[16] << 24 | (long)data[17] << 16 | (long)data[18] << 8 | data[19];
        var height = (long)data[20] << 24 | (long)data[21] << 16 | (long)data[22] << 8 | data[23];

        return (ClampToInt(width), ClampToInt(height));
    }

    private static (int Width, int Height)? ReadGifSize(ReadOnlySpan<byte> data)
    {
        if (data.Length < 10)
            return null;

        return (data[6] | data[7] << 8, data[8] | data[9] << 8);
    }

    // Идёт по сегментам до первого SOF (Start Of Frame), в котором лежат высота и ширина.
    private static (int Width, int Height)? ReadJpegSize(ReadOnlySpan<byte> data)
    {
        var i = 2;

        while (i + 3 < data.Length)
        {
            if (data[i] != 0xFF)
                return null;

            var marker = data[i + 1];

            if (marker == 0xFF)
            {
                i++;
                continue;
            }

            if (marker is 0xD8 or 0x01 or >= 0xD0 and <= 0xD7)
            {
                i += 2;
                continue;
            }

            if (marker is 0xD9 or 0xDA)
                return null;

            var segmentLength = data[i + 2] << 8 | data[i + 3];

            if (segmentLength < 2)
                return null;

            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                if (i + 9 > data.Length)
                    return null;

                return (data[i + 7] << 8 | data[i + 8], data[i + 5] << 8 | data[i + 6]);
            }

            i += 2 + segmentLength;
        }

        return null;
    }

    private static string? ValidateText(string? fileName, byte[] data)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".txt", StringComparison.OrdinalIgnoreCase))
            return UnsupportedType;

        if (data.Length > CommentLimits.AttachmentTextMaxBytes)
            return $"Текстовый файл не больше {CommentLimits.AttachmentTextMaxBytes / 1024} КБ.";

        string text;

        try
        {
            text = StrictUtf8.GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return "Текстовый файл должен быть в кодировке UTF-8.";
        }

        return text.Any(IsForbiddenControl) ? "В текстовом файле есть недопустимые символы." : null;
    }

    private static bool IsForbiddenControl(char c) => char.IsControl(c) && c is not ('\t' or '\n' or '\r');

    private static int ClampToInt(long value) => (int)Math.Min(value, int.MaxValue);
}

using System.Text;
using Comments.Api.Application.Validation;
using Comments.Api.Domain;

namespace Comments.Tests.Application.Validation;

public class AttachmentValidatorTests
{
    [Theory]
    [InlineData(320, 240)]
    [InlineData(1, 1)]
    [InlineData(100, 200)]
    public void Validate_AcceptsImagesWithinLimits(int width, int height)
    {
        Assert.Null(Check("a.png", Png(width, height)));
        Assert.Null(Check("a.gif", Gif(width, height)));
        Assert.Null(Check("a.jpg", Jpeg(width, height)));
    }

    [Theory]
    [InlineData(321, 240)]
    [InlineData(320, 241)]
    [InlineData(1920, 1080)]
    public void Validate_RejectsImagesLargerThanLimit(int width, int height)
    {
        Assert.Contains("320×240", Check("a.png", Png(width, height)));
        Assert.Contains("320×240", Check("a.gif", Gif(width, height)));
        Assert.Contains("320×240", Check("a.jpg", Jpeg(width, height)));
    }

    [Fact]
    public void Validate_DetectsImageByContent_NotByName()
    {
        Assert.Contains("320×240", Check("photo.txt", Png(1000, 1000)));
        Assert.Contains("320×240", Check("photo.exe", Jpeg(1000, 1000)));
    }

    [Fact]
    public void Validate_RejectsEmptyFile()
    {
        Assert.Contains("пустой", Check("a.txt", []));
    }

    [Fact]
    public void Validate_RejectsTooBigFile()
    {
        var data = Png(10, 10).Concat(new byte[CommentLimits.AttachmentImageMaxBytes]).ToArray();

        Assert.Contains("больше", Check("a.png", data));
    }

    [Fact]
    public void Validate_RejectsTruncatedImages()
    {
        Assert.Contains("размеры", Check("a.png", Png(10, 10)[..12]));
        Assert.Contains("размеры", Check("a.gif", Gif(10, 10)[..8]));
        Assert.Contains("размеры", Check("a.jpg", [0xFF, 0xD8, 0xFF, 0xE0]));
    }

    [Fact]
    public void Validate_RejectsPngWithoutIhdr()
    {
        var data = Png(10, 10);
        data[12] = (byte)'X';

        Assert.Contains("размеры", Check("a.png", data));
    }

    [Theory]
    [InlineData("Привет, мир")]
    [InlineData("строка 1\r\nстрока 2\tс табом")]
    public void Validate_AcceptsUtf8Text(string text)
    {
        Assert.Null(Check("notes.txt", Encoding.UTF8.GetBytes(text)));
        Assert.Null(Check("NOTES.TXT", Encoding.UTF8.GetBytes(text)));
    }

    [Fact]
    public void Validate_AcceptsTextWithBom()
    {
        var data = Encoding.UTF8.GetPreamble().Concat("текст"u8.ToArray()).ToArray();

        Assert.Null(Check("a.txt", data));
    }

    [Fact]
    public void Validate_AcceptsTextExactlyAtLimit_RejectsAbove()
    {
        var atLimit = new string('a', CommentLimits.AttachmentTextMaxBytes);

        Assert.Null(Check("a.txt", Encoding.UTF8.GetBytes(atLimit)));
        Assert.Contains("КБ", Check("a.txt", Encoding.UTF8.GetBytes(atLimit + "a")));
    }

    [Fact]
    public void Validate_RejectsNotUtf8Text()
    {
        var windows1251 = new byte[] { 0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2 };

        Assert.Contains("UTF-8", Check("a.txt", windows1251));
    }

    [Fact]
    public void Validate_RejectsBinaryNamedAsText()
    {
        Assert.Contains("недопустимые", Check("a.txt", [0x4D, 0x5A, 0x00, 0x01, 0x02]));
    }

    [Theory]
    [InlineData("a.exe")]
    [InlineData("a.pdf")]
    [InlineData("a.png")]
    [InlineData("noextension")]
    [InlineData(null)]
    public void Validate_RejectsUnknownContentWithoutTxtExtension(string? fileName)
    {
        Assert.Contains("JPG, GIF, PNG и TXT", Check(fileName, "просто текст"u8.ToArray()));
    }

    [Theory]
    [InlineData("a.png", ".png", AttachmentKind.Image)]
    [InlineData("a.jpg", ".jpg", AttachmentKind.Image)]
    [InlineData("a.gif", ".gif", AttachmentKind.Image)]
    public void Inspect_ReturnsKindAndExtensionByContent(string fileName, string extension, AttachmentKind kind)
    {
        var data = extension switch { ".png" => Png(10, 10), ".gif" => Gif(10, 10), _ => Jpeg(10, 10) };
        using var stream = new MemoryStream(data);

        var error = AttachmentValidator.Inspect(fileName, stream, out var inspection);

        Assert.Null(error);
        Assert.NotNull(inspection);
        Assert.Equal(kind, inspection.Kind);
        Assert.Equal(extension, inspection.Extension);
        Assert.Equal(data, inspection.Data);
    }

    [Fact]
    public void Inspect_ReturnsTextKind_ForTxt()
    {
        using var stream = new MemoryStream("привет"u8.ToArray());

        Assert.Null(AttachmentValidator.Inspect("a.txt", stream, out var inspection));
        Assert.Equal(AttachmentKind.Text, inspection!.Kind);
        Assert.Equal(".txt", inspection.Extension);
    }

    [Fact]
    public void Inspect_ReturnsNoResult_OnError()
    {
        using var stream = new MemoryStream(Png(1000, 1000));

        Assert.NotNull(AttachmentValidator.Inspect("a.png", stream, out var inspection));
        Assert.Null(inspection);
    }

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static string? Check(string? fileName, byte[] data)
    {
        using var stream = new MemoryStream(data);
        return AttachmentValidator.Validate(fileName, stream);
    }

    private static byte[] Png(int width, int height)
    {
        var data = new byte[33];
        PngSignature.CopyTo(data, 0);
        data[11] = 13;
        "IHDR"u8.CopyTo(data.AsSpan(12));
        WriteBigEndian(data, 16, width);
        WriteBigEndian(data, 20, height);

        return data;
    }

    private static byte[] Gif(int width, int height) =>
    [
        (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a',
        (byte)(width & 0xFF), (byte)(width >> 8), (byte)(height & 0xFF), (byte)(height >> 8),
        0, 0, 0
    ];

    private static byte[] Jpeg(int width, int height) =>
    [
        0xFF, 0xD8,
        0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
        0xFF, 0xC0, 0x00, 0x11, 0x08, (byte)(height >> 8), (byte)(height & 0xFF), (byte)(width >> 8), (byte)(width & 0xFF),
        0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01
    ];

    private static void WriteBigEndian(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}

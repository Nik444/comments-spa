using Comments.Api.Application.Abstractions;
using SkiaSharp;

namespace Comments.Api.Infrastructure.Captcha;

// Рисует код с наклоном и смещением каждой буквы, поверх точки и линии, чтобы его было сложно прочитать программой.
// Шрифт берётся из системы. В Docker-образе должен быть установлен шрифт (например, fonts-dejavu-core).
public sealed class SkiaCaptchaImageRenderer : ICaptchaImageRenderer
{
    private const int Width = 160;
    private const int Height = 56;
    private const float FontSize = 34;

    public byte[] Render(string code)
    {
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(new SKColor(245, 245, 245));

        DrawDots(canvas);
        DrawCode(canvas, code);
        DrawLines(canvas);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private static void DrawCode(SKCanvas canvas, string code)
    {
        using var typeface = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, FontSize);
        using var paint = new SKPaint { IsAntialias = true };

        var step = (float)Width / (code.Length + 1);

        for (var i = 0; i < code.Length; i++)
        {
            var x = step * (i + 0.5f) + Random.Shared.Next(-3, 4);
            var y = Height / 2f + FontSize / 3f + Random.Shared.Next(-5, 6);

            paint.Color = RandomColor(max: 120);

            canvas.Save();
            canvas.RotateDegrees(Random.Shared.Next(-20, 21), x + FontSize / 4f, y - FontSize / 3f);
            canvas.DrawText(code[i].ToString(), x, y, font, paint);
            canvas.Restore();
        }
    }

    private static void DrawDots(SKCanvas canvas)
    {
        using var paint = new SKPaint { IsAntialias = true };

        for (var i = 0; i < 80; i++)
        {
            paint.Color = RandomColor(max: 200);
            canvas.DrawCircle(Random.Shared.Next(Width), Random.Shared.Next(Height), Random.Shared.Next(1, 3), paint);
        }
    }

    private static void DrawLines(SKCanvas canvas)
    {
        using var paint = new SKPaint { IsAntialias = true, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke };

        for (var i = 0; i < 4; i++)
        {
            paint.Color = RandomColor(max: 160);
            canvas.DrawLine(0, Random.Shared.Next(Height), Width, Random.Shared.Next(Height), paint);
        }
    }

    // Тёмные цвета: чем меньше max, тем темнее.
    private static SKColor RandomColor(int max) =>
        new((byte)Random.Shared.Next(max), (byte)Random.Shared.Next(max), (byte)Random.Shared.Next(max));
}

namespace Comments.Api.Application.Abstractions;

public interface ICaptchaImageRenderer
{
    // Возвращает PNG с нарисованным кодом.
    byte[] Render(string code);
}

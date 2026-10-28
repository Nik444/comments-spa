namespace Comments.Api.Application.Exceptions;

public sealed class InvalidCaptchaException() : Exception("Неверный код с картинки или он устарел. Запросите новую картинку.");

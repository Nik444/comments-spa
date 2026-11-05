namespace Comments.Api.Application.Responses;

/// <param name="Id">Идентификатор капчи, возвращается вместе с ответом при отправке формы.</param>
/// <param name="Image">Картинка в виде data URL, её можно сразу подставить в src у img.</param>
public sealed record CaptchaResponse(string Id, string Image);

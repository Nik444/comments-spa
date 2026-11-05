namespace Comments.Api.Application.Responses;

/// <param name="Kind">"image" или "text".</param>
/// <param name="Url">Адрес файла на этом же сайте.</param>
public sealed record AttachmentResponse(string Kind, string Url, string Name, long Size);

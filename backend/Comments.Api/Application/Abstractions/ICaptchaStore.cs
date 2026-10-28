namespace Comments.Api.Application.Abstractions;

public interface ICaptchaStore
{
    Task SaveAsync(string id, string code, TimeSpan lifetime, CancellationToken cancellationToken);

    // Отдаёт код и сразу удаляет его: одна капча даёт одну попытку, подбирать код нельзя.
    Task<string?> TakeAsync(string id, CancellationToken cancellationToken);
}

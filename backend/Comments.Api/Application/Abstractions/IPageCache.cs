namespace Comments.Api.Application.Abstractions;

public interface IPageCache
{
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CancellationToken cancellationToken);

    // Только первые и последние страницы в пределах списка. Страницы из середины и за пределами идут в базу.
    bool IsCacheablePage(int page, int totalPages);
}

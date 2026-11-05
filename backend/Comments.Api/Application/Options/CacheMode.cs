namespace Comments.Api.Application.Options;

// Off: без кэша. Memory: только память сервера. Hybrid: память и Redis.
public enum CacheMode
{
    Off,
    Memory,
    Hybrid
}

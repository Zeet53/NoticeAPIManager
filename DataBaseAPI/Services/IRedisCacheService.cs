namespace DataBaseAPI.Services;

public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value);
    Task<T?> GetAsync<T>(string key) where T : class;
    Task DeleteAsync(string key);
}

using StackExchange.Redis;
using System.Text.Json;

namespace DataBaseAPI.Services;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase? _db;
    private bool _available;

    public RedisCacheService(IConfiguration configuration)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("Redis")
                ?? configuration["Redis"]
                ?? "localhost:6379,abortConnect=false";
            var redis = ConnectionMultiplexer.Connect(connectionString);
            _db = redis.GetDatabase();
            _available = true;
            Console.WriteLine("[RedisCacheService] Redis connected successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RedisCacheService] Redis not available, caching disabled: {ex.Message}");
            _available = false;
            _db = null;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        if (!_available || _db == null) return;
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RedisCacheService] Set error ({key}): {ex.Message}");
        }
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_available || _db == null) return null;
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RedisCacheService] Get error ({key}): {ex.Message}");
            return null;
        }
    }

    public async Task DeleteAsync(string key)
    {
        if (!_available || _db == null) return;
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RedisCacheService] Delete error ({key}): {ex.Message}");
        }
    }
}

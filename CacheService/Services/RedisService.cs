using StackExchange.Redis;
using System.Text.Json;

namespace CacheService.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("Redis")
                ?? "localhost:6379";
            var redis = ConnectionMultiplexer.Connect(connectionString);
            _db = redis.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl, When.Always, CommandFlags.None);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task<bool> DeleteAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }
    }
}

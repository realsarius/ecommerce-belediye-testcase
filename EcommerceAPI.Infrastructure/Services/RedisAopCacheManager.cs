using EcommerceAPI.Core.CrossCuttingConcerns.Caching;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EcommerceAPI.Infrastructure.Services;

public class RedisAopCacheManager : ICacheManager
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;

    public RedisAopCacheManager(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
    }

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto
    };

    public void Add(string key, object value, int duration)
    {
        var jsonData = JsonConvert.SerializeObject(value, JsonSettings);
        _database.StringSet(key, jsonData, TimeSpan.FromMinutes(duration));
    }

    public T Get<T>(string key)
    {
        var redisValue = _database.StringGet(key);
        if (redisValue.HasValue)
        {
            return JsonConvert.DeserializeObject<T>(redisValue.ToString(), JsonSettings)!;
        }

        return default!;
    }

    public object Get(string key)
    {
        var redisValue = _database.StringGet(key);
        if (redisValue.HasValue)
        {
            return JsonConvert.DeserializeObject(redisValue.ToString(), JsonSettings)!;
        }

        return null!;
    }

    public object Get(string key, Type type)
    {
        var redisValue = _database.StringGet(key);
        if (redisValue.HasValue)
        {
            return JsonConvert.DeserializeObject(redisValue.ToString(), type, JsonSettings)!;
        }

        return null!;
    }

    public bool IsAdd(string key)
    {
        return _database.KeyExists(key);
    }

    public void Remove(string key)
    {
        _database.KeyDelete(key);
    }

    public void RemoveByPattern(string pattern)
    {
        var endpoint = _connectionMultiplexer.GetEndPoints().FirstOrDefault();
        if (endpoint == null)
        {
            return;
        }

        var server = _connectionMultiplexer.GetServer(endpoint);
        foreach (var key in server.Keys(pattern: $"*{pattern}*"))
        {
            _database.KeyDelete(key);
        }
    }
}

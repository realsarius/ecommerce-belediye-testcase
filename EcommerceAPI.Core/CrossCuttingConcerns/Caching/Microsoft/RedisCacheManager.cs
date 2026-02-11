
using Newtonsoft.Json;
using StackExchange.Redis;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching;

namespace EcommerceAPI.Core.CrossCuttingConcerns.Caching.Microsoft;

public class RedisCacheManager : ICacheManager
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;

    public RedisCacheManager(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
    }

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.All
    };

    public void Add(string key, object value, int duration)
    {
        var jsonData = JsonConvert.SerializeObject(value, _jsonSettings);
        _database.StringSet(key, jsonData, TimeSpan.FromMinutes(duration));
    }

    public T Get<T>(string key)
    {
        var redisValue = _database.StringGet(key);
        if (redisValue.HasValue)
        {
            return JsonConvert.DeserializeObject<T>(redisValue.ToString(), _jsonSettings);
        }
        return default;
    }

    public object Get(string key)
    {
        var redisValue = _database.StringGet(key);
        if (redisValue.HasValue)
        {
             return JsonConvert.DeserializeObject(redisValue.ToString(), _jsonSettings);
        }
        return null;
    }

    public object Get(string key, Type type)
    {
        var redisValue = _database.StringGet(key);
        if (redisValue.HasValue)
        {
            return JsonConvert.DeserializeObject(redisValue.ToString(), type, _jsonSettings);
        }
        return null;
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
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().FirstOrDefault());
        foreach (var key in server.Keys(pattern: "*" + pattern + "*"))
        {
            _database.KeyDelete(key);
        }
    }
}

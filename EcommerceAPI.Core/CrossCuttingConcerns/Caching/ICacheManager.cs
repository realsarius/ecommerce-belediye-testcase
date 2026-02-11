
namespace EcommerceAPI.Core.CrossCuttingConcerns.Caching;

public interface ICacheManager
{
    T Get<T>(string key);
    object Get(string key);
    object Get(string key, Type type);
    void Add(string key, object value, int duration);
    bool IsAdd(string key);
    void Remove(string key);
    void RemoveByPattern(string pattern);
}

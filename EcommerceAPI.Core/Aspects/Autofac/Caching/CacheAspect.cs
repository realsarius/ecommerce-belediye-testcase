
using System.Reflection;
using Castle.DynamicProxy;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching;
using EcommerceAPI.Core.Utilities.Interceptors;
using EcommerceAPI.Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EcommerceAPI.Core.Aspects.Autofac.Caching;

public class CacheAspect : MethodInterception
{
    private readonly int _duration;
    private ICacheManager? _cacheManager;

    public CacheAspect(int duration = 60)
    {
        _duration = duration;
    }

    private ICacheManager CacheManager =>
        _cacheManager ??= ServiceTool.ServiceProvider.GetRequiredService<ICacheManager>();

    public override void Intercept(IInvocation invocation)
    {
        var cacheManager = CacheManager;

        var reflectedTypeName = invocation.Method.ReflectedType?.FullName ?? invocation.Method.DeclaringType?.FullName ?? "UnknownType";
        var methodName = $"{reflectedTypeName}.{invocation.Method.Name}";
        var arguments = invocation.Arguments.ToList();
        var key = $"{methodName}({string.Join(",", arguments.Select(x => x != null ? JsonConvert.SerializeObject(x, Formatting.None) : "<Null>"))})";

        var isAsync = (invocation.Method.ReturnType == typeof(Task) ||
                       (invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));

        if (isAsync)
        {
            if (invocation.Method.ReturnType.IsGenericType)
            {
                var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
                if (cacheManager.IsAdd(key))
                {
                    try
                    {
                        var cachedValue = cacheManager.Get(key, returnType);
                        if (cachedValue != null)
                        {
                            var method = typeof(Task).GetMethod(nameof(Task.FromResult))?.MakeGenericMethod(returnType);
                            invocation.ReturnValue = method?.Invoke(null, new[] { cachedValue }) ??
                                throw new InvalidOperationException("Task.FromResult invoker oluşturulamadı.");
                            return;
                        }

                        cacheManager.Remove(key);
                    }
                    catch
                    {
                        cacheManager.Remove(key);
                    }
                }

                invocation.Proceed();
                if (invocation.ReturnValue is not Task task)
                {
                    throw new InvalidOperationException("Beklenen Task sonucu elde edilemedi.");
                }

                var handleMethod = GetType().GetMethod(nameof(HandleAsyncWithResult), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(returnType)
                    ?? throw new InvalidOperationException("HandleAsyncWithResult metodu bulunamadı.");
                invocation.ReturnValue = handleMethod.Invoke(this, new object[] { task, key, _duration }) ??
                    throw new InvalidOperationException("Async cache handler çağrısı başarısız.");
            }
            else
            {
                invocation.Proceed();
            }
        }
        else
        {
            if (cacheManager.IsAdd(key))
            {
                try
                {
                    var cachedValue = cacheManager.Get(key, invocation.Method.ReturnType);
                    if (cachedValue != null)
                    {
                        invocation.ReturnValue = cachedValue;
                        return;
                    }

                    cacheManager.Remove(key);
                }
                catch
                {
                    cacheManager.Remove(key);
                }
            }

            invocation.Proceed();
            if (invocation.ReturnValue != null)
            {
                cacheManager.Add(key, invocation.ReturnValue, _duration);
            }
        }
    }

    private async Task<T> HandleAsyncWithResult<T>(Task<T> task, string key, int duration)
    {
        var result = await task;
        if (result != null)
        {
            CacheManager.Add(key, result, duration);
        }

        return result;
    }
}

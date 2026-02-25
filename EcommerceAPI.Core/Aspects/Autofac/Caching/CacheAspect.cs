
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
    private int _duration;
    private ICacheManager _cacheManager;

    public CacheAspect(int duration = 60)
    {
        _duration = duration;
    }

    public override void Intercept(IInvocation invocation)
    {
        if (_cacheManager == null)
        {
             _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>();
        }

        var methodName = string.Format($"{invocation.Method.ReflectedType.FullName}.{invocation.Method.Name}");
        var arguments = invocation.Arguments.ToList();
        var key = $"{methodName}({string.Join(",", arguments.Select(x => x != null ? JsonConvert.SerializeObject(x, Formatting.None) : "<Null>"))})";

        var isAsync = (invocation.Method.ReturnType == typeof(Task) ||
                       (invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));

        if (isAsync)
        {
            if (invocation.Method.ReturnType.IsGenericType)
            {
                var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
                if (_cacheManager.IsAdd(key))
                {
                    try
                    {
                        var cachedValue = _cacheManager.Get(key, returnType);
                        if (cachedValue != null)
                        {
                            var method = typeof(Task).GetMethod(nameof(Task.FromResult))?.MakeGenericMethod(returnType);
                            invocation.ReturnValue = method?.Invoke(null, new[] { cachedValue });
                            return;
                        }

                        _cacheManager.Remove(key);
                    }
                    catch
                    {
                        _cacheManager.Remove(key);
                    }
                }

                invocation.Proceed();
                var task = (Task)invocation.ReturnValue;

                var handleMethod = GetType().GetMethod(nameof(HandleAsyncWithResult), BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(returnType);
                invocation.ReturnValue = handleMethod.Invoke(this, new object[] { task, key, _duration });
            }
            else
            {
                 invocation.Proceed();
            }
        }
        else
        {
            if (_cacheManager.IsAdd(key))
            {
                try
                {
                    var cachedValue = _cacheManager.Get(key, invocation.Method.ReturnType);
                    if (cachedValue != null)
                    {
                        invocation.ReturnValue = cachedValue;
                        return;
                    }

                    _cacheManager.Remove(key);
                }
                catch
                {
                    _cacheManager.Remove(key);
                }
            }

            invocation.Proceed();
            _cacheManager.Add(key, invocation.ReturnValue, _duration);
        }
    }

    private async Task<T> HandleAsyncWithResult<T>(Task<T> task, string key, int duration)
    {
        var result = await task;
        _cacheManager.Add(key, result, duration);
        return result;
    }
}

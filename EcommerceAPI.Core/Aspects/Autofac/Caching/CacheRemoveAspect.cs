
using Castle.DynamicProxy;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching;
using EcommerceAPI.Core.Utilities.Interceptors;
using EcommerceAPI.Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Core.Aspects.Autofac.Caching;

public class CacheRemoveAspect : MethodInterception
{
    private readonly string _pattern;
    private ICacheManager? _cacheManager;

    public CacheRemoveAspect(string pattern)
    {
        _pattern = pattern;
    }

    private ICacheManager CacheManager =>
        _cacheManager ??= ServiceTool.ServiceProvider.GetRequiredService<ICacheManager>();

    protected override void OnSuccess(IInvocation invocation)
    {
        CacheManager.RemoveByPattern(_pattern);
    }
}

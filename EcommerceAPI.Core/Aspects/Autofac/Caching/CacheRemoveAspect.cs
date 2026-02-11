
using Castle.DynamicProxy;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching;
using EcommerceAPI.Core.Utilities.Interceptors;
using EcommerceAPI.Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Core.Aspects.Autofac.Caching;

public class CacheRemoveAspect : MethodInterception
{
    private string _pattern;
    private ICacheManager _cacheManager;

    public CacheRemoveAspect(string pattern)
    {
        _pattern = pattern;
    }

    protected override void OnSuccess(IInvocation invocation)
    {
        if (_cacheManager == null)
        {
             _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>();
        }

        _cacheManager.RemoveByPattern(_pattern);
    }
}

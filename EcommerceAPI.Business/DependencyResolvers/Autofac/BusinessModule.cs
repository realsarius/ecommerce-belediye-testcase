
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Concrete;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching;
using EcommerceAPI.Core.CrossCuttingConcerns.Caching.Microsoft;
using EcommerceAPI.Core.Utilities.Interceptors;

namespace EcommerceAPI.Business.DependencyResolvers.Autofac;

public class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(assembly)
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors(new ProxyGenerationOptions()
            {
                Selector = new AspectInterceptorSelector()
            })
            .InstancePerLifetimeScope();

        builder.RegisterType<RedisCacheManager>().As<ICacheManager>().SingleInstance();
    }
}

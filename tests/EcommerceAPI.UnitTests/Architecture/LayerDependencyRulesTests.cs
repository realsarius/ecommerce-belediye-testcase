using FluentAssertions;

namespace EcommerceAPI.UnitTests.Architecture;

public class LayerDependencyRulesTests
{
    [Fact]
    public void Business_ShouldNotReference_DataAccess()
    {
        var referencedAssemblies = GetReferencedAssemblyNames(typeof(EcommerceAPI.Business.DependencyInjection).Assembly);

        referencedAssemblies.Should().NotContain("EcommerceAPI.DataAccess");
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_Business()
    {
        var referencedAssemblies = GetReferencedAssemblyNames(typeof(EcommerceAPI.Infrastructure.DependencyInjection).Assembly);

        referencedAssemblies.Should().NotContain("EcommerceAPI.Business");
    }

    [Fact]
    public void DataAccess_ShouldNotReference_Business()
    {
        var referencedAssemblies = GetReferencedAssemblyNames(typeof(EcommerceAPI.DataAccess.DependencyInjection).Assembly);

        referencedAssemblies.Should().NotContain("EcommerceAPI.Business");
    }

    [Fact]
    public void Core_ShouldNotReference_EntityFrameworkCore()
    {
        var referencedAssemblies = GetReferencedAssemblyNames(typeof(EcommerceAPI.Core.Utilities.IoC.ServiceTool).Assembly);

        referencedAssemblies.Should().NotContain("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Core_ShouldNotReference_StackExchangeRedis()
    {
        var referencedAssemblies = GetReferencedAssemblyNames(typeof(EcommerceAPI.Core.Utilities.IoC.ServiceTool).Assembly);

        referencedAssemblies.Should().NotContain("StackExchange.Redis");
    }

    private static HashSet<string> GetReferencedAssemblyNames(System.Reflection.Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

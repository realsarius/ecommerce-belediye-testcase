using FluentAssertions;
namespace EcommerceAPI.ArchitectureTests;

public class LayerDependencyTests
{
    [Fact]
    public void Business_Should_Not_Depend_On_DataAccess()
    {
        var referencedAssemblies = typeof(EcommerceAPI.Business.DependencyInjection)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        referencedAssemblies.Should().NotContain("EcommerceAPI.DataAccess");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Business_Assembly()
    {
        var referencedAssemblies = typeof(EcommerceAPI.Infrastructure.DependencyInjection)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        referencedAssemblies.Should().NotContain("EcommerceAPI.Business");
    }

    [Fact]
    public void Core_Should_Not_Depend_On_Forbidden_Framework_Packages()
    {
        var forbiddenDependencies = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql.EntityFrameworkCore",
            "StackExchange.Redis"
        };

        var referencedAssemblies = typeof(EcommerceAPI.Core.Utilities.Results.IResult)
            .Assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        referencedAssemblies.Should().NotIntersectWith(forbiddenDependencies);
    }
}

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private Respawner _respawner = default!;
    private DbConnection _connection = default!;

    public DatabaseFixture(CustomWebApplicationFactory factory)
        => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        _connection = new NpgsqlConnection(db.Database.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")]
        });
    }

    public async Task ResetAsync()
        => await _respawner.ResetAsync(_connection);

    public async Task DisposeAsync()
        => await _connection.DisposeAsync();
}

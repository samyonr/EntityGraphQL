using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tests.DataAccess;

namespace EntityGraphQL.AspNet.Extra.Tests.Infra;

public sealed class DbFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:13")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
        ConnectionString = _dbContainer.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    public TestsAppDbContext CreateDbContext()
    {
        string? connectionString = _dbContainer.GetConnectionString();
        DbContextOptions<TestsAppDbContext> options = new DbContextOptionsBuilder<TestsAppDbContext>()
            .UseNpgsql(connectionString + ";Include Error Detail=true")
            .Options;
        return new TestsAppDbContext(options);
    }
}
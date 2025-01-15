using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tests.DataAccess;

namespace EntityGraphQL.AspNet.Extra.Tests.Infra;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _environment;

    public CustomWebApplicationFactory(string connectionString, string environment)
    {
        _connectionString = connectionString;
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:AnimalsDbConnectionString", _connectionString },
                { "ApiKeys:Reader:0", "TestReaderApiKey" },
                { "ApiKeys:Reader:1", "TestWriterApiKey" },
                { "ApiKeys:Writer:0", "TestWriterApiKey" }
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            ServiceDescriptor? descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TestsAppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test connection string
            services.AddDbContext<TestsAppDbContext>(options => { options.UseNpgsql(_connectionString); });

            // Ensure the database is created
            ServiceProvider sp = services.BuildServiceProvider();
            using IServiceScope scope = sp.CreateScope();
            TestsAppDbContext db = scope.ServiceProvider.GetRequiredService<TestsAppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
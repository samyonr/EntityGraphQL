using EntityGraphQL.AspNet;
using EntityGraphQL.Schema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Tests.App.Entry;
using Tests.App.Entry.Auth;
using Tests.App.Entry.Exceptions;
using Tests.DataAccess;
using Tests.Entities;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IWebHostEnvironment env = builder.Environment;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

if (!env.IsEnvironment("Test"))
{
    builder.Configuration
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.Scheme;
        options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.Scheme;
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.Scheme, options =>
        {
            Dictionary<string, string[]> roleBasedApiKeys = builder.Configuration.GetSection("ApiKeys").Get<Dictionary<string, string[]>>() ??
                                                            throw new EnvironmentException("ApiKeys configuration is incorrect");

            // Transform to a mapping of API keys to roles
            Dictionary<string, List<string>> apiKeyRoles = new();
            foreach (KeyValuePair<string, string[]> role in roleBasedApiKeys)
            {
                foreach (string apiKey in role.Value)
                {
                    if (!apiKeyRoles.TryGetValue(apiKey, out List<string>? value))
                    {
                        value = [];
                        apiKeyRoles[apiKey] = value;
                    }

                    value.Add(role.Key);
                }
            }

            options.ApiKeyRoles = apiKeyRoles.ToDictionary(k => k.Key, v => v.Value.ToArray());
        });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireReaderOrWriterRole", policy => policy.RequireRole("Reader", "Writer"))
    .AddPolicy("RequireWriterRole", policy => policy.RequireRole("Writer"))
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Reader", "Writer")
        .Build());

builder.Services.AddGraphQLSchema<TestsAppDbContext>(GraphQlConfiguration.ConfigureSchema);
builder.Services.AddGraphQLValidator();

builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();

if (env.IsDevelopment())
{
    // 1. Grab your existing connection string from appsettings.Development.json
    string existingConnectionString = builder.Configuration.GetConnectionString("AnimalsDbConnectionString")
                                      ?? throw new Exception("Connection string 'AnimalsDbConnectionString' not found.");

    // 2. Parse it to get username, password, database, etc.
    NpgsqlConnectionStringBuilder npgsqlBuilder = new(existingConnectionString);

    // 3. Build a Testcontainers Postgres with ephemeral port
    PostgreSqlContainer? dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:13")
        .WithUsername(npgsqlBuilder.Username)
        .WithPassword(npgsqlBuilder.Password)
        .WithDatabase(npgsqlBuilder.Database)
        .WithExposedPort(5432) // Let Docker pick a free host port
        .Build();

    await dbContainer.StartAsync();

    // 4. Retrieve the ephemeral host port that Docker mapped to container port 5432
    int assignedPort = dbContainer.GetMappedPublicPort(5432);

    // 5. Update the existing connection string's Port to the ephemeral port
    npgsqlBuilder.Port = assignedPort;

    // 6. Save the updated ephemeral-based connection string
    string ephemeralConnectionString = npgsqlBuilder.ConnectionString;

    // 7. Register DbContext *using* that ephemeral port
    builder.Services.AddDbContext<TestsAppDbContext>(options =>
        options.UseNpgsql(ephemeralConnectionString));

    // 8. Now run migrations to ensure DB is up-to-date
    DbContextOptions<TestsAppDbContext> dbOptions = new DbContextOptionsBuilder<TestsAppDbContext>()
        .UseNpgsql(ephemeralConnectionString)
        .Options;

    await using TestsAppDbContext dbContext = new(dbOptions);
    IMigrator migrator = dbContext.Database.GetService<IMigrator>();

    // (a) Wipe the DB (if you need a clean slate in dev)
    await migrator.MigrateAsync("0");

    // (b) Re-apply all migrations
    await migrator.MigrateAsync();

    // (c) Optionally seed some data
    dbContext.People.Add(new Person { Name = "John Doe", Age = 30 });
    dbContext.SaveChanges();
}
else
{
    // In non-development environments, just use the normal connection string from config
    builder.Services.AddDbContext<TestsAppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("AnimalsDbConnectionString")));
}

WebApplication app = builder.Build();


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/schema", (SchemaProvider<TestsAppDbContext> schema) => schema.ToGraphQLSchemaString())
    .RequireAuthorization(new AuthorizeAttribute { Roles = "Reader,Writer" });

switch (env.EnvironmentName)
{
    case "Test_WithoutFollowSpec":
        app.MapGraphQL<TestsAppDbContext>("/graphql", configureEndpoint: endpoint => endpoint.RequireAuthorization());
        break;
    case "Test_WithFollowSpec":
        app.MapGraphQL<TestsAppDbContext>("/graphql", followSpec: true, configureEndpoint: endpoint => endpoint.RequireAuthorization());
        break;
    case "Test_WithCustom":
        app.CustomMapGraphQL<TestsAppDbContext>("/graphql", configureEndpoint: endpoint => endpoint.RequireAuthorization());
        break;
    default:
        app.CustomMapGraphQL<TestsAppDbContext>("/graphql", configureEndpoint: endpoint => endpoint.RequireAuthorization());
        break;
}

app.MapControllers().RequireAuthorization();

await app.RunAsync();

public partial class Program;
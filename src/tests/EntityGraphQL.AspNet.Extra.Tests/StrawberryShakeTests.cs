using EntityGraphQL.AspNet.Extra.Tests.Infra;
using EntityGraphQL.AspNet.Extra.Tests.State;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using Tests.DataAccess;

namespace EntityGraphQL.AspNet.Extra.Tests;

public class StrawberryShakeTests : IClassFixture<DbFixture>
{
    private TestsAppDbContext _dbContext = null!;
    private readonly DbFixture _dbFixture;
    private CustomWebApplicationFactory _factory = null!;
    private readonly ITestOutputHelper _testOutputHelper;
    private IAppClient _client = null!;
    private readonly VerifySettings _verifySettings;

    public StrawberryShakeTests(DbFixture dbFixture, ITestOutputHelper testOutputHelper)
    {
        _dbFixture = dbFixture;
        _testOutputHelper = testOutputHelper;
        _verifySettings = GetVerifySettings();
    }

    private async Task InitializeAsync(string environment)
    {
        _dbContext = _dbFixture.CreateDbContext();
        await DbUtils.ApplyMigrationsAsync(_dbContext);

        string connectionString = _dbFixture.ConnectionString;

        _factory = new CustomWebApplicationFactory(connectionString, environment);

        ServiceCollection servicesCollection = new();

        IClientBuilder<AppClientStoreAccessor> clientBuilder = servicesCollection.AddAppClient();

        clientBuilder.ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(_factory.Server.BaseAddress, "graphql");
                client.DefaultRequestHeaders.Add("x-key", "TestWriterApiKey");
            },
            httpClientBuilder =>
            {
                // Use the TestServer's handler to direct requests to the in-memory server
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => _factory.Server.CreateHandler());
                httpClientBuilder.AddHttpMessageHandler(() => new LoggingHandler(_testOutputHelper));
            });

        ServiceProvider services = servicesCollection.BuildServiceProvider();
        _client = services.GetRequiredService<IAppClient>();
    }

    private async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await DbUtils.ClearDatabaseAsync(_dbContext);
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData("Test_WithCustom", "Custom")]
    [InlineData("Test_WithoutFollowSpec", "WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec", "WithFollowSpec")]
    public async Task Http200Ok(string environment, string testCase)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            IOperationResult<IAddPersonResult> addPersonResult = await _client.AddPerson.ExecuteAsync(new PersonInputModel
            {
                Name = "Test Person",
                Age = -30
            }, TestContext.Current.CancellationToken);

            // Act
            IOperationResult<IGetPeopleResult> getPersonResult = await _client.GetPeople.ExecuteAsync(TestContext.Current.CancellationToken);

            // Assert
            VerifySettings settings = GetVerifySettings();
            settings.UseFileName(
                $"{nameof(StrawberryShakeTests)}.{nameof(Http200Ok)}.{testCase}");
            await Verify(new
            {
                addPersonResult,
                getPersonResult
            }, settings);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    private static VerifySettings GetVerifySettings()
    {
        VerifySettings settings = new();
        settings.UseDirectory("VerifySnapshots");
        settings.AddExtraSettings(serializerSettings => { serializerSettings.ContractResolver = new OrderedContractResolver(); });
        return settings;
    }
}
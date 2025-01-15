using System.Net;
using System.Net.Http.Json;
using EntityGraphQL.AspNet.Extra.Tests.Infra;
using Tests.DataAccess;

namespace EntityGraphQL.AspNet.Extra.Tests;

public class AuthTests : IClassFixture<DbFixture>
{
    private CustomWebApplicationFactory _factory = null!;
    private TestsAppDbContext _dbContext = null!;
    private readonly DbFixture _dbFixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public AuthTests(DbFixture dbFixture, ITestOutputHelper testOutputHelper)
    {
        _dbFixture = dbFixture;
        _testOutputHelper = testOutputHelper;
    }

    private async Task InitializeAsync(string environment)
    {
        _dbContext = _dbFixture.CreateDbContext();
        await DbUtils.ApplyMigrationsAsync(_dbContext);

        string connectionString = _dbFixture.ConnectionString;

        _factory = new CustomWebApplicationFactory(connectionString, environment);
    }

    private async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await DbUtils.ClearDatabaseAsync(_dbContext);
        await _dbContext.DisposeAsync();
    }


    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task Query_WithReaderRole_ShouldSucceed(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-key", "TestReaderApiKey");
            const string query = "{ people { id } }";

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/graphql", new { query },
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("errors", content);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task Query_WithWriterRole_ShouldSucceed(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-key", "TestWriterApiKey");

            const string query = "{ people { id } }";

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/graphql", new { query },
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("errors", content);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task Query_WithNoRole_ShouldFail(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            // No API key provided

            const string query = "{ people { id } }";

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/graphql", new { query },
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task Mutation_WithWriterRole_ShouldSucceed(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            LoggingHandler handler = new(_testOutputHelper)
            {
                InnerHandler = _factory.Server.CreateHandler()
            };

            HttpClient httpClient = new(handler)
            {
                BaseAddress = _factory.Server.BaseAddress
            };

            httpClient.DefaultRequestHeaders.Add("x-key", "TestWriterApiKey");

            const string mutation = """
                                    mutation {
                                        addPerson(
                                            inputModel: {
                                                name: "John",
                                                age: 30
                                            }
                                        ) {
                                            id
                                        }
                                    }
                                    """;

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/graphql", new { query = mutation },
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("errors", content);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task Mutation_WithReaderRole_ShouldFail(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            LoggingHandler handler = new(_testOutputHelper)
            {
                InnerHandler = _factory.Server.CreateHandler()
            };

            HttpClient httpClient = new(handler)
            {
                BaseAddress = _factory.Server.BaseAddress
            };

            httpClient.DefaultRequestHeaders.Add("x-key", "TestReaderApiKey");

            const string mutation = """
                                    mutation {
                                        addPerson(
                                            inputModel: {
                                                name: "John",
                                                age: 30
                                            }
                                        ) {
                                            id
                                        }
                                    }
                                    """;

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/graphql", new { query = mutation },
                cancellationToken: TestContext.Current.CancellationToken);

            // Now make sure the request itself is OK, and should succeed with the writer role
            httpClient.DefaultRequestHeaders.Remove("x-key");
            httpClient.DefaultRequestHeaders.Add("x-key", "TestWriterApiKey");

            HttpResponseMessage response2 = await httpClient.PostAsJsonAsync("/graphql", new { query = mutation },
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode(); // GraphQL returns 200 with errors
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("Error occurred", content);

            response2.EnsureSuccessStatusCode();
            string content2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain("errors", content2);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task Mutation_WithNoRole_ShouldFail(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            // No API key provided

            const string mutation = """
                                    mutation {
                                        addPerson(
                                            inputModel: {
                                                name: "John",
                                                age: 30
                                            }
                                        ) {
                                            id
                                        }
                                    }
                                    """;

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/graphql", new { query = mutation },
                cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task SchemaEndpoint_WithWriterRole_ShouldSucceed(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-key", "TestWriterApiKey"); // Reader key

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/schema", TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.NotEmpty(content);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task SchemaEndpoint_WithReaderRole_ShouldSucceed(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-key", "TestReaderApiKey");

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/schema", TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.NotEmpty(content);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Test_WithCustom")]
    [InlineData("Test_WithoutFollowSpec")]
    [InlineData("Test_WithFollowSpec")]
    public async Task SchemaEndpoint_WithNoRole_ShouldFail(string environment)
    {
        try
        {
            // Arrange
            await InitializeAsync(environment);
            HttpClient httpClient = _factory.CreateClient();
            // No API key provided

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/schema", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await DisposeAsync();
        }
    }
}
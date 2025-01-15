namespace EntityGraphQL.AspNet.Extra.Tests.Infra;

public class LoggingHandler : DelegatingHandler
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LoggingHandler(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Log the request content
        if (request.Content != null)
        {
            string requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            _testOutputHelper.WriteLine("Request:");
            _testOutputHelper.WriteLine(requestContent);
        }
        else
        {
            _testOutputHelper.WriteLine("Request with no content");
        }

        // Send the request to the next handler in the pipeline
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // Log the response content
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _testOutputHelper.WriteLine("Response:");
        _testOutputHelper.WriteLine(responseContent);

        return response;
    }
}
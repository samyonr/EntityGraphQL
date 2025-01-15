using EntityGraphQL;
using EntityGraphQL.AspNet;
using EntityGraphQL.Schema;

namespace Tests.App.Entry;

public static class CustomEntityGraphQlEndpointRouteExtensions
{
    private const string APP_JSON_TYPE_START = "application/json";

    public static IEndpointRouteBuilder CustomMapGraphQL<TQueryType>(
        this IEndpointRouteBuilder builder,
        string path = "graphql",
        ExecutionOptions? options = null,
        Action<IEndpointConventionBuilder>? configureEndpoint = null
    )
    {
        path = path.TrimEnd('/');
        IEndpointConventionBuilder postEndpoint = builder.MapPost(
            path,
            async context =>
            {
                if (context.Request.ContentType?.StartsWith(APP_JSON_TYPE_START,
                        StringComparison.InvariantCulture) == false)
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return;
                }

                if (context.Request.ContentLength == null || context.Request.ContentLength == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                IGraphQLRequestDeserializer deserializer = context.RequestServices.GetRequiredService<IGraphQLRequestDeserializer>();
                QueryRequest query = await deserializer.DeserializeAsync(context.Request.Body);

                SchemaProvider<TQueryType> schema =
                    context.RequestServices.GetService<SchemaProvider<TQueryType>>()
                    ?? throw new InvalidOperationException(
                        "No SchemaProvider<TQueryType> found in the service collection. Make sure you set up your Startup.ConfigureServices() to call AddGraphQLSchema<TQueryType>()."
                    );
                QueryResult data = await schema.ExecuteRequestAsync(query, context.RequestServices, context.User, options);
                context.Response.ContentType = "application/json; charset=utf-8";
                if (data.Errors?.Count > 0)
                {
                    // according to the spec: https://spec.graphql.org/October2021/#sec-Errors
                    // we should return 200 status code for errors
                    // and data shouldn't appear for "Request errors"
                    // data can't be null either in that case
                    // setting 200 explicitly, although it's the default value, and it's not modified above
                    // except for the cases that return early
                    // and it's not modified internally because it's not available internally
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    data = new QueryResult(data.Errors);
                }

                IGraphQLResponseSerializer serializer = context.RequestServices.GetRequiredService<IGraphQLResponseSerializer>();
                await serializer.SerializeAsync(context.Response.Body, data);
            }
        );

        configureEndpoint?.Invoke(postEndpoint);

        return builder;
    }
}
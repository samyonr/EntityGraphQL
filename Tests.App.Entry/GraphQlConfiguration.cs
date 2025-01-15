using System.Reflection;
using EntityGraphQL.AspNet;
using EntityGraphQL.Schema;
using EntityGraphQL.Schema.FieldExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tests.App.Entry.API.Mutations;
using Tests.DataAccess;
using Tests.Entities;

namespace Tests.App.Entry;

public static class GraphQlConfiguration
{
    public static void ConfigureSchema(AddGraphQLOptions<TestsAppDbContext> options)
    {
        options.IgnoreTypes.Add(typeof(IModel));
        options.IgnoreProps.Remove("Model");
        options.ConfigureSchema = schema =>
        {
            schema.Query().RequiredAuthorization =
                new RequiredAuthorization(roles: new List<List<string>> { new() { "Reader", "Writer" } },
                    policies: null);

            schema.UpdateQuery(queryType =>
            {
                // Find all entities in the DbContext that have a DbSet
                IEnumerable<PropertyInfo> dbSetProperties = typeof(TestsAppDbContext)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.IsGenericType
                                && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

                foreach (PropertyInfo prop in dbSetProperties)
                {
                    Type entityType = prop.PropertyType.GetGenericArguments()[0];

                    // Check if the entity implements IEntity
                    if (!typeof(IEntity).IsAssignableFrom(entityType))
                    {
                        continue;
                    }

                    AddFilteredFieldToType(entityType, queryType);
                }
            });

            AddMutations(schema);

            schema.Mutation().SchemaType.RequiredAuthorization =
                new RequiredAuthorization(roles: new List<List<string>> { new() { "Writer" } }, policies: null);

            AddPetsImplementations(schema);
        };
    }

    private static void AddPetsImplementations(SchemaProvider<TestsAppDbContext> schema)
    {
        schema.Type<Dog>(nameof(Dog))
            .Implements<Pet>();
        schema.Type<Cat>(nameof(Cat))
            .Implements<Pet>();
    }

    private static void AddFilteredFieldToType(Type entityType, SchemaType<TestsAppDbContext> queryType)
    {
        MethodInfo method = typeof(GraphQlConfiguration).GetMethod(nameof(AddFilteredField),
            BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo genericMethod = method.MakeGenericMethod(entityType);
        genericMethod.Invoke(null, [queryType]);
    }

    private static void AddFilteredField<TEntity>(SchemaType<TestsAppDbContext> queryType)
        where TEntity : class, IEntity
    {
        // Add a filter field

        string fieldName = char.ToLower(typeof(TEntity).Name[0]) + typeof(TEntity).Name[1..] + "s";
        string description = $"Return all {typeof(TEntity).Name}s using a filter";

        Field field = queryType.ReplaceField<IQueryable<TEntity>>(
            fieldName,
            db => db.Set<TEntity>(),
            description
        );

        field.UseFilter();
    }

    private static void AddMutations(SchemaProvider<TestsAppDbContext> schema)
    {
        schema.AddMutationsFrom<IGraphQlMutation>();
    }
}
using System.Linq.Expressions;
using EntityGraphQL;
using EntityGraphQL.Schema;
using Tests.DataAccess;
using Tests.Entities;

namespace Tests.App.Entry.API.Mutations;

[GraphQLInputType]
public sealed class PersonInputModel
{
    public required string Name { get; init; }
    public required int Age { get; init; }
}

public sealed class PersonMutations(
    TestsAppDbContext db) : IGraphQlMutation
{
    [GraphQLMutation("Add person")]
    public async Task<Expression<Func<TestsAppDbContext, Person>>?> AddPerson(
        PersonInputModel inputModel, IGraphQLValidator validator)
    {
        if (inputModel.Age < 0)
        {
            validator.AddError("Age must be greater than or equal to 0");
            return null;
        }

        db.People.Add(new Person
        {
            Name = inputModel.Name,
            Age = inputModel.Age
        });
        await db.SaveChangesAsync();

        return ctx => ctx.People.First(p => p.Name == inputModel.Name);
    }
}
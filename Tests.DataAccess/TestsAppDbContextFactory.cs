using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tests.DataAccess;

/// <summary>
///     This class is used run migrations from the command line in local development.
///     Run the following command in the terminal to execute the migrations in localhost:
///     dotnet ef database update
/// </summary>
public class TestsAppDbContextFactory : IDesignTimeDbContextFactory<TestsAppDbContext>
{
    public TestsAppDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<TestsAppDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(
            "Server=localhost;Port=5432;Database=TestDb;User Id=postgres;Password=pass123;Include Error Detail=true;");
        return new TestsAppDbContext(optionsBuilder.Options);
    }
}
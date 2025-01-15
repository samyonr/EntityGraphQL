using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tests.DataAccess;

namespace EntityGraphQL.AspNet.Extra.Tests.Infra;

public static class DbUtils
{
    public static async Task ApplyMigrationsAsync(TestsAppDbContext dbContext)
    {
        IMigrator migrator = dbContext.Database.GetService<IMigrator>();
        await migrator
            .MigrateAsync("0"); // Reset the database to its initial state. Revert any applied migration, if any
        await migrator.MigrateAsync();
    }

    public static async Task ClearDatabaseAsync(TestsAppDbContext dbContext)
    {
        IMigrator migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("0"); // Reset the database to its initial state
    }
}
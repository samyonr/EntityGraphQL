using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tests.Entities;

namespace Tests.DataAccess;

public class TestsAppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Dog> Dogs { get; set; }
    public DbSet<Cat> Cats { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("AnimalsDb");

        IEnumerable<IMutableEntityType> entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(IEntity).IsAssignableFrom(t.ClrType) && !t.ClrType.IsInterface);

        foreach (Type clrType in entityTypes.Select(e => e.ClrType))
        {
            modelBuilder.Entity(clrType, entity => { entity.Property<int>(nameof(IEntity.Id)).ValueGeneratedOnAdd(); });
            modelBuilder.Entity(clrType).UseTpcMappingStrategy();
        }
    }
}
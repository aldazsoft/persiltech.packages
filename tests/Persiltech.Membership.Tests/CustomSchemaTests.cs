namespace Persiltech.Membership.Tests;

/// <summary>
/// Contexto del consumidor que coloca la membresía en un esquema propio.
/// </summary>
public sealed class CustomSchemaDbContext(DbContextOptions<CustomSchemaDbContext> options)
    : MembershipDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("membership");

        base.OnModelCreating(builder);
    }
}

/// <summary>
/// Fija que el consumidor puede mover la membresía a su propio esquema derivando el
/// contexto, sin que el paquete tenga que ofrecer un parámetro para ello.
/// </summary>
public sealed class CustomSchemaTests
{
    [Fact]
    public void ElConsumidorPuedeElegirElEsquema()
    {
        var options = new DbContextOptionsBuilder<CustomSchemaDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new CustomSchemaDbContext(options);

        var tables = context.Model.GetEntityTypes()
            .Select(entity => $"{entity.GetSchema() ?? "(por defecto)"}.{entity.GetTableName()}")
            .OrderBy(name => name)
            .ToList();

        Assert.All(tables, table => Assert.StartsWith("membership.", table));
        Assert.Contains("membership.MembershipRefreshTokens", tables);
        Assert.Contains("membership.AspNetUsers", tables);
    }
}

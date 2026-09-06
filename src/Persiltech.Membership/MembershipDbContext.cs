namespace Persiltech.Membership;

/// <summary>
/// Contexto de datos de ASP.NET Core Identity sobre el usuario que elija el consumidor.
/// </summary>
/// <typeparam name="TUser">
/// Usuario de la aplicación. <see cref="ApplicationUser"/> o una clase derivada de ella.
/// </typeparam>
/// <param name="options">
/// Opciones del contexto, con el proveedor de Entity Framework Core que eligió el consumidor.
/// </param>
/// <remarks>
/// Es <c>abstract</c> porque nunca se instancia por sí sola: o se usa
/// <see cref="MembershipDbContext"/>, que es la concreción para el caso corriente, o se
/// deriva de ella con el usuario propio. Toma <see cref="DbContextOptions"/> a secas y no la
/// forma genérica porque el tipo cerrado lo aporta la clase derivada, que es la que registra
/// <c>AddDbContext</c>.
/// </remarks>
public abstract class MembershipDbContext<TUser>(DbContextOptions options)
    : IdentityDbContext<TUser>(options) where TUser : ApplicationUser
{
    /// <inheritdoc />
    /// <remarks>
    /// La entidad de los testigos de renovación se configura aquí y no con un
    /// <c>DbSet&lt;&gt;</c> público porque es interna: el consumidor la ve en su migración,
    /// que es donde tiene que verla, y no la nombra en su código.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(token =>
        {
            token.ToTable("MembershipRefreshTokens");
            token.HasKey(t => t.Id);

            token.Property(t => t.UserId).IsRequired();
            token.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);

            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.UserId);
            token.HasIndex(t => t.FamilyId);

            token.HasOne<TUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>
/// Contexto de datos de Identity sobre <see cref="ApplicationUser"/>.
/// </summary>
/// <param name="options">
/// Opciones del contexto, con el proveedor de Entity Framework Core que eligió el consumidor.
/// </param>
/// <remarks>
/// Es la concreción para quien no necesite extender el usuario. Quien sí lo necesite deriva
/// su propio contexto de <see cref="MembershipDbContext{TUser}"/>.
/// </remarks>
public sealed class MembershipDbContext(DbContextOptions<MembershipDbContext> options)
    : MembershipDbContext<ApplicationUser>(options);

namespace Persiltech.Membership.OAuth;

/// <summary>
/// Contexto de datos del servidor de autorización. Contiene únicamente las entidades de
/// OpenIddict: aplicaciones cliente, autorizaciones, ámbitos y testigos.
/// </summary>
/// <param name="options">
/// Opciones del contexto, con el proveedor de Entity Framework Core que eligió el
/// consumidor.
/// </param>
/// <remarks>
/// Es un contexto aparte del <see cref="MembershipDbContext"/> del paquete base, y no una
/// ampliación suya: aquel es <c>sealed</c> y no declara las entidades de OpenIddict, y
/// hacerlo obligaría al paquete base a depender de OpenIddict aunque no se use. Ambos
/// pueden apuntar a la misma base de datos; cada uno lleva sus propias migraciones.
/// </remarks>
public sealed class MembershipOAuthDbContext(DbContextOptions<MembershipOAuthDbContext> options)
    : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseOpenIddict();
    }
}

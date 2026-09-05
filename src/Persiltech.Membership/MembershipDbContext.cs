namespace Persiltech.Membership;

/// <summary>
/// Contexto de datos de ASP.NET Core Identity sobre <see cref="ApplicationUser"/>.
/// </summary>
/// <param name="options">
/// Opciones del contexto, con el proveedor de Entity Framework Core que eligió el consumidor.
/// </param>
/// <remarks>
/// No declara conjuntos propios ni sobrescribe el modelo: el esquema es el estándar de
/// Identity más las dos columnas de <see cref="ApplicationUser"/>. Se expone para que el
/// consumidor genere sus migraciones contra él.
/// </remarks>
public sealed class MembershipDbContext(DbContextOptions<MembershipDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);

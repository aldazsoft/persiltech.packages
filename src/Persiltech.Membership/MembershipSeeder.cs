namespace Persiltech.Membership;

/// <summary>
/// Siembra la cuenta de administración con la que se destraba una instalación nueva.
/// </summary>
/// <remarks>
/// Sin ella el sistema queda en un punto muerto: los endpoints de administración exigen la
/// política que ponga el consumidor, y el endpoint que crearía el primer rol de
/// administrador exigiría ya serlo.
/// </remarks>
public static class MembershipSeeder
{
    /// <summary>
    /// Crea el rol y la cuenta de administración si aún no existen.
    /// </summary>
    /// <param name="provider">Proveedor de servicios de la aplicación consumidora.</param>
    /// <param name="administrator">Datos de la cuenta de administración.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>
    /// <see langword="true"/> si esta llamada creó la cuenta; <see langword="false"/> si ya
    /// existía.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// No se pudo crear el rol o la cuenta. Se lanza a propósito en lugar de devolver un
    /// resultado: ocurre en el arranque, y una instalación sin administrador no debe quedar
    /// en pie disimulando el fallo.
    /// </exception>
    /// <remarks>
    /// Es idempotente y **no toca la cuenta si ya existe**: en particular, no reescribe su
    /// contraseña. Así, dejar la llamada en el arranque no revierte en cada despliegue la
    /// contraseña que el administrador haya cambiado.
    /// </remarks>
    public static async Task<bool> SeedMembershipAdministratorAsync(
        this IServiceProvider provider,
        MembershipAdministrator administrator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(administrator);

        using var scope = provider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        cancellationToken.ThrowIfCancellationRequested();

        if (!await roleManager.RoleExistsAsync(administrator.RoleName))
        {
            var createdRole = await roleManager.CreateAsync(new IdentityRole(administrator.RoleName));

            if (!createdRole.Succeeded)
            {
                throw new InvalidOperationException(Describe("el rol de administración", createdRole));
            }
        }

        var existing = await userManager.FindByEmailAsync(administrator.Email);

        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, administrator.RoleName))
            {
                var assigned = await userManager.AddToRoleAsync(existing, administrator.RoleName);

                if (!assigned.Succeeded)
                {
                    throw new InvalidOperationException(Describe("el rol a la cuenta existente", assigned));
                }
            }

            return false;
        }

        var user = new ApplicationUser
        {
            UserName = administrator.Email,
            Email = administrator.Email,
            EmailConfirmed = true,
            FirstName = administrator.FirstName,
            LastName = administrator.LastName
        };

        var createdUser = await userManager.CreateAsync(user, administrator.Password);

        if (!createdUser.Succeeded)
        {
            throw new InvalidOperationException(Describe("la cuenta de administración", createdUser));
        }

        var addedToRole = await userManager.AddToRoleAsync(user, administrator.RoleName);

        if (!addedToRole.Succeeded)
        {
            throw new InvalidOperationException(Describe("el rol a la cuenta", addedToRole));
        }

        return true;
    }

    private static string Describe(string what, IdentityResult result) =>
        $"No se pudo crear {what}: {string.Join(" ", result.Errors.Select(e => e.Description))}";
}

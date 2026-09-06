namespace Persiltech.Membership.Internal;

/// <summary>
/// Resuelve la cuenta a la que pertenece el token de la petición.
/// </summary>
internal static class CurrentUser
{
    /// <summary>
    /// Busca el usuario por la reclamación <see cref="ClaimTypes.Name"/>, que lleva su
    /// correo.
    /// </summary>
    /// <param name="principal">Identidad de la petición.</param>
    /// <param name="userManager">Gestor de usuarios de Identity.</param>
    /// <typeparam name="TUser">Usuario de la aplicación.</typeparam>
    /// <returns>El usuario, o <see langword="null"/> si no hay reclamación o no existe.</returns>
    /// <remarks>
    /// Se resuelve por el correo y no por el identificador porque el token que emite el
    /// paquete no lleva el identificador, y añadirlo sería un cambio de contrato. Como el
    /// correo es a la vez el nombre de usuario, identifica la cuenta igual de bien.
    /// </remarks>
    internal static async Task<TUser?> FindAsync<TUser>(
        ClaimsPrincipal principal,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var email = principal.FindFirstValue(ClaimTypes.Name);

        return string.IsNullOrEmpty(email) ? null : await userManager.FindByEmailAsync(email);
    }
}

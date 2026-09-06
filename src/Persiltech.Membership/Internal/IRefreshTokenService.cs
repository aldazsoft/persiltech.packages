namespace Persiltech.Membership.Internal;

/// <summary>
/// Emite, rota y revoca testigos de renovación.
/// </summary>
/// <remarks>
/// Aísla el manejador HTTP del almacén, igual que <see cref="IAccessTokenFactory"/> lo aísla
/// del formato del token.
/// </remarks>
internal interface IRefreshTokenService
{
    /// <summary>
    /// Emite el primer testigo de una familia nueva, para un inicio de sesión.
    /// </summary>
    /// <param name="userId">Cuenta que inicia la sesión.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>El testigo en claro, que es lo único que verá el cliente.</returns>
    Task<string> IssueAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Consume el testigo presentado y emite el siguiente de su familia.
    /// </summary>
    /// <param name="refreshToken">Testigo en claro que presenta el cliente.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>
    /// El identificador de la cuenta y el testigo nuevo, o <see langword="null"/> si el
    /// presentado no vale.
    /// </returns>
    /// <remarks>
    /// Presentar un testigo ya consumido revoca la familia entera: no se puede distinguir
    /// de un robo, así que se asume lo peor.
    /// </remarks>
    Task<RotatedRefreshToken?> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revoca la familia entera a la que pertenece el testigo presentado.
    /// </summary>
    /// <param name="refreshToken">Testigo en claro que presenta el cliente.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>La tarea que representa la revocación. No falla si el testigo no existe.</returns>
    Task RevokeFamilyAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revoca todas las sesiones vivas de una cuenta.
    /// </summary>
    /// <param name="userId">Cuenta cuyas sesiones se cierran.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>La tarea que representa la revocación.</returns>
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken);
}


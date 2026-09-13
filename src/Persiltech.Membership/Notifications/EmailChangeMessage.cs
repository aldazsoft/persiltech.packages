namespace Persiltech.Membership.Notifications;

/// <summary>
/// Aviso de confirmación de un cambio de correo.
/// </summary>
/// <param name="UserId">Identificador de la cuenta.</param>
/// <param name="NewEmail">
/// Correo <em>nuevo</em>, que es el destinatario del aviso: es la dirección que hay que
/// demostrar que pertenece al usuario.
/// </param>
/// <param name="FirstName">Nombre del usuario, para personalizar el saludo.</param>
/// <param name="LastName">Apellido del usuario, para personalizar el saludo.</param>
/// <param name="Token">Testigo de cambio de correo que genera ASP.NET Core Identity.</param>
public sealed record EmailChangeMessage(
    string UserId,
    string NewEmail,
    string FirstName,
    string LastName,
    string Token)
{
    /// <summary>
    /// Aplicación cliente desde la que se pidió el aviso, tal como llegó en la cabecera
    /// <see cref="MembershipHeaders.ClientId"/>.
    /// </summary>
    /// <remarks>
    /// Es una propiedad aparte y opcional para no romper a quien ya construyera el mensaje por
    /// posición. Vacía significa "no se sabe", y quien redacte el correo usará su dirección de
    /// por defecto.
    /// </remarks>
    public string? ClientKey { get; init; }
}


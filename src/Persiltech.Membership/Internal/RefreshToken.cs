namespace Persiltech.Membership.Internal;

/// <summary>
/// Testigo de renovación tal como se guarda en <c>MembershipRefreshTokens</c>.
/// </summary>
/// <remarks>
/// Es interna a propósito: el consumidor no la consulta, no la escribe y no la recibe en
/// ninguna respuesta. Hacerla pública ataría el esquema al contrato y convertiría cualquier
/// columna nueva en un cambio de versión mayor.
/// <para>
/// Nunca guarda el testigo en claro, solo su SHA-256: quien consiga leer la tabla no
/// obtiene sesiones utilizables.
/// </para>
/// </remarks>
internal sealed class RefreshToken
{
    /// <summary>Clave primaria.</summary>
    public Guid Id { get; set; }

    /// <summary>Cuenta a la que pertenece la sesión.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 del testigo, en hexadecimal.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Agrupa todas las rotaciones que descienden de un mismo inicio de sesión.
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>Instante de emisión, en UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Instante en que deja de valer, en UTC.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Instante en que se rotó. <see langword="null"/> mientras no se haya usado.
    /// </summary>
    public DateTimeOffset? ConsumedAt { get; set; }

    /// <summary>
    /// Instante en que se revocó. <see langword="null"/> mientras siga vigente.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}

namespace Persiltech.Membership;

/// <summary>
/// Opciones de emisión del token de acceso.
/// </summary>
/// <remarks>
/// Se validan al arrancar la aplicación, no en la primera petición: el registro encadena
/// <c>ValidateOnStart()</c> sobre <see cref="Internal.JwtOptionsValidator"/>, de modo que una
/// configuración incompleta detiene el arranque en lugar de fallar al autenticar al primero
/// que lo intente.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>
    /// Clave simétrica con la que se firma el token. Obligatoria, mínimo 32 caracteres.
    /// </summary>
    /// <remarks>
    /// El mínimo no es arbitrario: HMAC-SHA256 exige una clave de al menos 256 bits, y una
    /// cadena de 32 caracteres ASCII es exactamente eso. Es un secreto que aporta el
    /// consumidor desde su configuración; el paquete no lo registra ni lo devuelve en
    /// ninguna respuesta.
    /// </remarks>
    public string SecurityKey { get; set; } = string.Empty;

    /// <summary>
    /// Emisor que viaja en la reclamación <c>iss</c>. Obligatorio.
    /// </summary>
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiencia que viaja en la reclamación <c>aud</c>. Obligatoria.
    /// </summary>
    public string ValidAudience { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de vigencia del token desde su emisión. Obligatorio, mayor que cero.
    /// </summary>
    public int ExpireInMinutes { get; set; }

    /// <summary>
    /// Días de vigencia del testigo de renovación. Obligatorio, mayor que cero.
    /// </summary>
    /// <remarks>
    /// Vive aquí y no en unas opciones propias porque las dos vigencias se eligen juntas:
    /// son los dos extremos de la misma sesión, y separarlas invitaría a configurar una y
    /// olvidar la otra.
    /// </remarks>
    public int RefreshTokenExpireInDays { get; set; } = 14;
}

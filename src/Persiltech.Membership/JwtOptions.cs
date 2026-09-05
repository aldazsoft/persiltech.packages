namespace Persiltech.Membership;

/// <summary>
/// Opciones de emisión del token de acceso.
/// </summary>
/// <remarks>
/// Se validan al arrancar la aplicación, no en la primera petición: el registro encadena
/// <c>ValidateDataAnnotations().ValidateOnStart()</c>, de modo que una violación de
/// restricción detiene el arranque.
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
    [Required]
    [MinLength(32)]
    public string SecurityKey { get; set; } = string.Empty;

    /// <summary>
    /// Emisor que viaja en la reclamación <c>iss</c>. Obligatorio.
    /// </summary>
    [Required]
    public string ValidIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiencia que viaja en la reclamación <c>aud</c>. Obligatoria.
    /// </summary>
    [Required]
    public string ValidAudience { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de vigencia del token desde su emisión. Obligatorio, mayor que cero.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExpireInMinutes { get; set; }
}

namespace Persiltech.Email;

/// <summary>
/// Cómo se cifra la conexión con el servidor SMTP.
/// </summary>
/// <remarks>
/// Es un tipo propio y no el de la biblioteca de transporte: así el arranque del consumidor
/// no necesita conocerla, y el paquete puede cambiarla sin romper el contrato.
/// </remarks>
public enum SmtpSecurity
{
    /// <summary>
    /// Se decide por el puerto y por lo que anuncie el servidor. Valor por defecto.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Sin cifrado. Solo para un relay local de desarrollo.
    /// </summary>
    None = 1,

    /// <summary>
    /// Conexión en claro que se eleva a TLS con <c>STARTTLS</c>. Lo habitual en el 587.
    /// </summary>
    StartTls = 2,

    /// <summary>
    /// TLS desde el primer byte. Lo habitual en el 465.
    /// </summary>
    SslOnConnect = 3
}

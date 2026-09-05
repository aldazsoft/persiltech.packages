namespace Persiltech.Email;

/// <summary>
/// Opciones de conexión con el servidor SMTP.
/// </summary>
/// <remarks>
/// Se validan al arrancar la aplicación, no en el primer envío: el registro encadena
/// <c>ValidateOnStart()</c> sobre un <see cref="IValidateOptions{TOptions}"/> propio, de modo
/// que un host vacío detiene el arranque en lugar de perder el primer correo que alguien
/// esperaba recibir. El validador devuelve **todos** los fallos juntos, para no descubrirlos
/// de uno en uno a base de reinicios.
/// </remarks>
public sealed class SmtpOptions
{
    /// <summary>
    /// Nombre o dirección del servidor SMTP. Obligatorio.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Puerto del servidor. Por defecto, 587. Entre 1 y 65535.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Cómo se cifra la conexión. Por defecto, <see cref="SmtpSecurity.Auto"/>.
    /// </summary>
    public SmtpSecurity Security { get; set; } = SmtpSecurity.Auto;

    /// <summary>
    /// Usuario con el que se autentica la conexión. Opcional.
    /// </summary>
    /// <remarks>
    /// Si viene vacío no se autentica, porque un relay local de desarrollo no lo exige. Si
    /// se rellena, <see cref="Password"/> pasa a ser obligatoria.
    /// </remarks>
    public string? UserName { get; set; }

    /// <summary>
    /// Contraseña con la que se autentica la conexión. Opcional salvo que haya
    /// <see cref="UserName"/>.
    /// </summary>
    /// <remarks>
    /// Es un secreto: lo aporta el consumidor desde su configuración, y el paquete no lo
    /// registra en ningún log.
    /// </remarks>
    public string? Password { get; set; }

    /// <summary>
    /// Dirección del remitente de todos los mensajes. Obligatoria.
    /// </summary>
    /// <remarks>
    /// Se valida con el mismo analizador que la compondrá al enviar, así que lo que aquí
    /// pase, pasa también en tiempo de ejecución.
    /// </remarks>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Nombre visible del remitente. Opcional.
    /// </summary>
    public string? FromDisplayName { get; set; }

    /// <summary>
    /// Espera máxima, en segundos, de las operaciones con el servidor. Por defecto, 30.
    /// Entre 1 y 600.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;
}

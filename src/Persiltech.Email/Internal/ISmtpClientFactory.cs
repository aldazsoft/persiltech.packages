namespace Persiltech.Email.Internal;

/// <summary>
/// Costura que aísla la creación del cliente SMTP.
/// </summary>
/// <remarks>
/// Existe para poder verificar el envío sin levantar un servidor: es el único punto del
/// paquete que nombra a la biblioteca de transporte al construirla.
/// </remarks>
internal interface ISmtpClientFactory
{
    /// <summary>
    /// Crea un cliente SMTP sin conectar.
    /// </summary>
    /// <returns>El cliente, que es de quien lo pide desecharlo.</returns>
    ISmtpClient Create();
}

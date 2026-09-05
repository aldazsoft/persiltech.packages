namespace Persiltech.Email.Internal;

/// <summary>
/// Criterio único con el que el paquete analiza direcciones de correo.
/// </summary>
/// <remarks>
/// Lo comparten el validador de las opciones y la composición del mensaje: si el remitente
/// se comprobara con un criterio y el destinatario con otro, una dirección que el arranque
/// da por mala sería aceptable en un envío, o al revés.
/// </remarks>
internal static class EmailAddressParsing
{
    /// <summary>
    /// El analizador acepta direcciones sin dominio por defecto —<c>juan</c> a secas le
    /// vale—, y una dirección así la rechaza cualquier servidor SMTP.
    /// </summary>
    internal static readonly ParserOptions ParserOptions =
        new() { AllowAddressesWithoutDomain = false };
}

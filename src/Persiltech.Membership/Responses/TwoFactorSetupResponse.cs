namespace Persiltech.Membership.Responses;

/// <summary>
/// Datos con los que el usuario da de alta su aplicación de autenticación.
/// </summary>
/// <param name="SharedKey">
/// Clave compartida en base32, la que se teclea en la aplicación de autenticación.
/// </param>
/// <param name="Email">Cuenta a la que pertenece la clave.</param>
/// <remarks>
/// No incluye la URI <c>otpauth://</c> ni el código QR: esa URI lleva el nombre del emisor,
/// que es la marca del consumidor, y componerla aquí obligaría al paquete a decidirla. Con
/// estos dos datos, el consumidor la arma en una línea.
/// </remarks>
public sealed record TwoFactorSetupResponse(string SharedKey, string Email);

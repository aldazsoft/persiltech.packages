namespace Persiltech.Email.Sample;

/// <summary>
/// Cuerpo de la petición con la que el consumidor pide un envío. Es del sample, no del
/// paquete: redactar el mensaje es del lado del consumidor.
/// </summary>
public sealed record SendEmailRequest(string To, string Subject, string HtmlBody, string? TextBody);

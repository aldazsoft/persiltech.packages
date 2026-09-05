namespace Persiltech.Membership.Email;

/// <summary>
/// Resultado de componer un aviso.
/// </summary>
/// <param name="Subject">Asunto ya sustituido.</param>
/// <param name="HtmlBody">Cuerpo HTML completo: el aviso ya envuelto en el diseño común.</param>
/// <param name="TextBody">
/// Cuerpo alternativo en texto plano. No es opcional: un correo transaccional sin parte de
/// texto pierde reputación y se ve mal en los clientes que no muestran HTML.
/// </param>
public sealed record RenderedEmail(string Subject, string HtmlBody, string TextBody);

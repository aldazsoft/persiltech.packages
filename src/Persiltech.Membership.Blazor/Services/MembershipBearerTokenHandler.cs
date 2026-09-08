namespace Persiltech.Membership.Blazor.Services;

/// <summary>
/// Firma cada petición saliente con el token de acceso de la sesión.
/// </summary>
/// <remarks>
/// Se engancha al <see cref="HttpClient"/> con nombre del paquete, no al del consumidor: un
/// manejador global mandaría el token a cualquier dominio al que la aplicación llamara. Es
/// público para que un consumidor pueda engancharlo también a su propio cliente, si su API
/// acepta el mismo token.
/// </remarks>
/// <param name="tokens">Almacén del que sale el token.</param>
public sealed class MembershipBearerTokenHandler(IMembershipTokenStore tokens) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Una cabecera ya puesta gana: si el llamante decidió firmar de otra forma, no se le
        // pisa.
        if (request.Headers.Authorization is null)
        {
            var stored = await tokens.GetAsync();

            if (stored is not null && !string.IsNullOrWhiteSpace(stored.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stored.AccessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

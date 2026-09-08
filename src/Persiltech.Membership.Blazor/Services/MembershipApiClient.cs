namespace Persiltech.Membership.Blazor.Services;

/// <summary>
/// Cliente HTTP de la API de <c>Persiltech.Membership</c>.
/// </summary>
/// <remarks>
/// Toda llamada pasa por <c>SendAsync</c>, que traduce la respuesta a un
/// <see cref="ApiResult{T}"/>. Ese punto único es lo que hace que un <c>400</c> con
/// <c>ValidationProblemDetails</c> llegue a la pantalla con sus campos en lugar de como una
/// excepción sin contexto.
/// </remarks>
/// <param name="httpClient">Cliente con nombre del paquete, ya con su dirección base.</param>
/// <param name="options">Rutas en las que el consumidor montó los endpoints.</param>
public sealed class MembershipApiClient(
    HttpClient httpClient,
    IOptions<MembershipApiOptions> options) : IMembershipApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<ApiResult<MembershipTokens>> LoginAsync(LoginUserRequest request) =>
        SendAsync<MembershipTokens>(HttpMethod.Post, options.Value.LoginPath, request);

    /// <inheritdoc />
    public Task<ApiResult<Unit>> RegisterAsync(RegisterUserRequest request) =>
        SendAsync<Unit>(HttpMethod.Post, options.Value.RegisterPath, request);

    /// <inheritdoc />
    public Task<ApiResult<MembershipTokens>> RefreshAsync(string refreshToken) =>
        SendAsync<MembershipTokens>(
            HttpMethod.Post,
            options.Value.RefreshPath,
            new { refreshToken });

    /// <inheritdoc />
    public Task<ApiResult<Unit>> LogoutAsync(string refreshToken) =>
        SendAsync<Unit>(
            HttpMethod.Post,
            options.Value.LogoutPath,
            new { refreshToken });

    /// <inheritdoc />
    public Task<ApiResult<UserResponse>> GetCurrentUserAsync() =>
        SendAsync<UserResponse>(HttpMethod.Get, $"{options.Value.UsersPath}/current", null);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (HttpRequestException exception)
        {
            // Con la API apagada o sin CORS, el navegador falla aquí. Decirlo así ahorra
            // buscar el problema en el formulario.
            return ApiResult<T>.Failure(
                Single($"No se pudo contactar con la API en {httpClient.BaseAddress}. " +
                       $"¿Está en marcha y con CORS habilitado? ({exception.Message})"),
                HttpStatusCode.ServiceUnavailable);
        }

        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<T>.Failure(await ReadErrorsAsync(response), response.StatusCode);
        }

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength is 0 or null)
        {
            return ApiResult<T>.Success(default, response.StatusCode);
        }

        return ApiResult<T>.Success(
            await response.Content.ReadFromJsonAsync<T>(Json),
            response.StatusCode);
    }

    private static async Task<IReadOnlyDictionary<string, string[]>> ReadErrorsAsync(
        HttpResponseMessage response)
    {
        // El 401 no trae ValidationProblemDetails: lo emite el middleware de autenticación,
        // o el endpoint de renovación ante un testigo que no vale.
        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            return Single("La sesión no es válida o ha caducado. Vuelve a autenticarte.");
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(Json);

            if (problem?.Errors is { Count: > 0 })
            {
                return problem.Errors;
            }
        }
        catch (JsonException)
        {
            // Un cuerpo que no es ValidationProblemDetails cae al mensaje de abajo.
        }

        return Single($"La API respondió {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    private static Dictionary<string, string[]> Single(string message) =>
        new() { [string.Empty] = [message] };

    private sealed record ValidationProblem(Dictionary<string, string[]>? Errors);
}

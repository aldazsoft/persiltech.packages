namespace Persiltech.Membership.Blazor.Sample.Services;

/// <summary>
/// Cliente de la API de Persiltech.Membership: una operación por endpoint publicado.
/// </summary>
/// <remarks>
/// Todas las llamadas pasan por <see cref="SendAsync{T}"/>, que traduce la respuesta a un
/// <see cref="ApiResult{T}"/>. Ese punto único es lo que hace que un 400 con
/// ValidationProblemDetails llegue a la pantalla con sus campos en lugar de como una
/// excepción sin contexto.
/// </remarks>
public sealed class MembershipApiClient(HttpClient http, TokenStore tokens)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // --- Cuenta -------------------------------------------------------------

    public Task<ApiResult<object>> RegisterAsync(RegisterUserRequest request) =>
        SendAsync<object>(HttpMethod.Post, "user/register", request);

    public Task<ApiResult<LoginUserResponse>> LoginAsync(LoginUserRequest request) =>
        SendAsync<LoginUserResponse>(HttpMethod.Post, "user/login", request);

    public Task<ApiResult<string>> WhoAmIAsync() =>
        SendAsync<string>(HttpMethod.Get, "user/me", null, raw: true);

    // --- Contraseña ---------------------------------------------------------

    public Task<ApiResult<object>> ForgotPasswordAsync(ForgotPasswordRequest request) =>
        SendAsync<object>(HttpMethod.Post, "password/forgot", request);

    public Task<ApiResult<object>> ResetPasswordAsync(ResetPasswordRequest request) =>
        SendAsync<object>(HttpMethod.Post, "password/reset", request);

    public Task<ApiResult<object>> ChangePasswordAsync(ChangePasswordRequest request) =>
        SendAsync<object>(HttpMethod.Post, "password/change", request);

    // --- Correo -------------------------------------------------------------

    public Task<ApiResult<object>> SendEmailConfirmationAsync(SendEmailConfirmationRequest request) =>
        SendAsync<object>(HttpMethod.Post, "email/confirmation/send", request);

    public Task<ApiResult<object>> ConfirmEmailAsync(ConfirmEmailRequest request) =>
        SendAsync<object>(HttpMethod.Post, "email/confirmation", request);

    public Task<ApiResult<object>> ChangeEmailAsync(ChangeEmailRequest request) =>
        SendAsync<object>(HttpMethod.Post, "email/change", request);

    public Task<ApiResult<object>> ConfirmEmailChangeAsync(ConfirmEmailChangeRequest request) =>
        SendAsync<object>(HttpMethod.Post, "email/change/confirm", request);

    // --- Teléfono -----------------------------------------------------------

    public Task<ApiResult<object>> ChangePhoneNumberAsync(ChangePhoneNumberRequest request) =>
        SendAsync<object>(HttpMethod.Post, "phone/change", request);

    public Task<ApiResult<object>> ConfirmPhoneNumberChangeAsync(ConfirmPhoneNumberChangeRequest request) =>
        SendAsync<object>(HttpMethod.Post, "phone/change/confirm", request);

    // --- Perfil -------------------------------------------------------------

    public Task<ApiResult<object>> UpdateProfileAsync(UpdateProfileRequest request) =>
        SendAsync<object>(HttpMethod.Put, "profile", request);

    public Task<ApiResult<object>> DeleteAccountAsync() =>
        SendAsync<object>(HttpMethod.Delete, "profile", null);

    // --- Doble factor -------------------------------------------------------

    public Task<ApiResult<TwoFactorSetupResponse>> SetupTwoFactorAsync() =>
        SendAsync<TwoFactorSetupResponse>(HttpMethod.Post, "twofactor/setup", null);

    public Task<ApiResult<TwoFactorRecoveryCodesResponse>> EnableTwoFactorAsync(EnableTwoFactorRequest request) =>
        SendAsync<TwoFactorRecoveryCodesResponse>(HttpMethod.Post, "twofactor/enable", request);

    public Task<ApiResult<object>> DisableTwoFactorAsync() =>
        SendAsync<object>(HttpMethod.Post, "twofactor/disable", null);

    public Task<ApiResult<TwoFactorRecoveryCodesResponse>> RegenerateRecoveryCodesAsync() =>
        SendAsync<TwoFactorRecoveryCodesResponse>(HttpMethod.Post, "twofactor/recovery-codes", null);

    // --- Roles (administración) ---------------------------------------------

    public Task<ApiResult<PagedResponse<RoleResponse>>> GetRolesAsync(int page = 1, int pageSize = 20) =>
        SendAsync<PagedResponse<RoleResponse>>(HttpMethod.Get, $"roles/paged?page={page}&pageSize={pageSize}", null);

    public Task<ApiResult<RoleResponse>> GetRoleAsync(string id) =>
        SendAsync<RoleResponse>(HttpMethod.Get, $"roles/{id}", null);

    public Task<ApiResult<RoleResponse>> CreateRoleAsync(CreateRoleRequest request) =>
        SendAsync<RoleResponse>(HttpMethod.Post, "roles", request);

    public Task<ApiResult<object>> UpdateRoleAsync(string id, UpdateRoleRequest request) =>
        SendAsync<object>(HttpMethod.Put, $"roles/{id}", request);

    public Task<ApiResult<object>> DeleteRoleAsync(string id) =>
        SendAsync<object>(HttpMethod.Delete, $"roles/{id}", null);

    // --- Usuarios (administración) ------------------------------------------

    public Task<ApiResult<PagedResponse<UserResponse>>> GetUsersAsync(int page = 1, int pageSize = 20) =>
        SendAsync<PagedResponse<UserResponse>>(HttpMethod.Get, $"users/paged?page={page}&pageSize={pageSize}", null);

    public Task<ApiResult<UserResponse>> GetCurrentUserAsync() =>
        SendAsync<UserResponse>(HttpMethod.Get, "users/current", null);

    public Task<ApiResult<object>> AssignRolesAsync(string userId, AssignRolesRequest request) =>
        SendAsync<object>(HttpMethod.Put, $"users/{userId}/roles", request);

    public Task<ApiResult<object>> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request) =>
        SendAsync<object>(HttpMethod.Put, $"users/{userId}/status", request);

    /// <summary>
    /// Envía la petición con el token actual y traduce la respuesta a un resultado.
    /// </summary>
    /// <typeparam name="T">Tipo esperado del cuerpo cuando la llamada sale bien.</typeparam>
    /// <param name="method">Verbo HTTP.</param>
    /// <param name="path">Ruta relativa del endpoint.</param>
    /// <param name="body">Cuerpo a serializar, o <see langword="null"/>.</param>
    /// <param name="raw">Cuando el cuerpo de la respuesta es texto plano y no JSON.</param>
    /// <returns>El valor devuelto o los errores de validación.</returns>
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, bool raw = false)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        var token = await tokens.GetAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;

        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException exception)
        {
            // Con la API apagada o sin CORS, el navegador falla aquí. Decirlo así ahorra
            // buscar el problema en el formulario.
            return ApiResult<T>.Failure(
                $"No se pudo contactar con la API en {http.BaseAddress}. ¿Está en marcha y con CORS habilitado? ({exception.Message})",
                HttpStatusCode.ServiceUnavailable);
        }

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength is 0 or null)
            {
                return ApiResult<T>.Success(default, response.StatusCode);
            }

            if (raw)
            {
                var text = await response.Content.ReadAsStringAsync();

                return ApiResult<T>.Success((T)(object)text, response.StatusCode);
            }

            return ApiResult<T>.Success(
                await response.Content.ReadFromJsonAsync<T>(Json),
                response.StatusCode);
        }

        return ApiResult<T>.Failure(await ReadErrorsAsync(response), response.StatusCode);
    }

    private static async Task<IReadOnlyDictionary<string, string[]>> ReadErrorsAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            return new Dictionary<string, string[]>
            {
                [string.Empty] = ["La sesión no es válida o ha caducado. Vuelve a autenticarte."]
            };
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
            // Un cuerpo que no es ValidationProblemDetails cae al mensaje genérico de abajo.
        }

        return new Dictionary<string, string[]>
        {
            [string.Empty] = [$"La API respondió {(int)response.StatusCode} {response.ReasonPhrase}."]
        };
    }

    private sealed record ValidationProblem(
        [property: JsonPropertyName("errors")] Dictionary<string, string[]>? Errors);
}

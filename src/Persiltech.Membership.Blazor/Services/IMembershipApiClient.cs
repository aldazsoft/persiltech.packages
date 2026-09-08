namespace Persiltech.Membership.Blazor.Services;

/// <summary>
/// Cliente de la API de <c>Persiltech.Membership</c>.
/// </summary>
/// <remarks>
/// Ninguna operación lanza por un error HTTP: todas devuelven un
/// <see cref="ApiResult{T}"/> con el cuerpo o con los errores por campo. Es lo que permite
/// que un <c>400</c> llegue al formulario señalando qué está mal.
/// </remarks>
public interface IMembershipApiClient
{
    /// <summary>
    /// Autentica una cuenta.
    /// </summary>
    /// <param name="request">Credenciales, y el segundo factor si la cuenta lo exige.</param>
    /// <returns>El par de testigos recién emitido.</returns>
    Task<ApiResult<MembershipTokens>> LoginAsync(LoginUserRequest request);

    /// <summary>
    /// Crea una cuenta.
    /// </summary>
    /// <param name="request">Correo, contraseña y nombre.</param>
    /// <returns>Sin cuerpo: la API responde <c>201</c>.</returns>
    Task<ApiResult<Unit>> RegisterAsync(RegisterUserRequest request);

    /// <summary>
    /// Renueva la sesión.
    /// </summary>
    /// <param name="refreshToken">Testigo de renovación vigente.</param>
    /// <returns>
    /// El par nuevo. El testigo presentado queda consumido, valga o no la llamada.
    /// </returns>
    Task<ApiResult<MembershipTokens>> RefreshAsync(string refreshToken);

    /// <summary>
    /// Cierra la sesión en el servidor, revocando la familia del testigo.
    /// </summary>
    /// <param name="refreshToken">Testigo de renovación de la sesión.</param>
    /// <returns>Sin cuerpo: la API responde <c>204</c> valga o no el testigo.</returns>
    Task<ApiResult<Unit>> LogoutAsync(string refreshToken);

    /// <summary>
    /// Obtiene la cuenta a la que pertenece el token de la petición.
    /// </summary>
    /// <returns>El usuario y sus roles.</returns>
    Task<ApiResult<UserResponse>> GetCurrentUserAsync();

    /// <summary>
    /// Pide el correo con el que reiniciar una contraseña olvidada.
    /// </summary>
    /// <param name="request">Correo de la cuenta.</param>
    /// <returns>
    /// Sin cuerpo. La API responde 204 exista o no la cuenta, para no revelar qué correos
    /// están registrados.
    /// </returns>
    Task<ApiResult<Unit>> ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Fija una contraseña nueva con el testigo que llegó por correo.
    /// </summary>
    /// <param name="request">Correo, testigo y contraseña nueva.</param>
    /// <returns>Sin cuerpo: la API responde 204.</returns>
    Task<ApiResult<Unit>> ResetPasswordAsync(ResetPasswordRequest request);
}

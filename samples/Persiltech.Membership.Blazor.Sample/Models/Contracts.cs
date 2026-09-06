namespace Persiltech.Membership.Blazor.Sample.Models;

// Los contratos se redeclaran aquí en lugar de referenciar el paquete: un frontend de
// terceros solo conoce el JSON que la API publica, y copiarlo es lo que pone a prueba que
// ese contrato baste. Referenciar los tipos del servidor ocultaría cualquier desajuste.

public sealed record RegisterUserRequest(string Email, string Password, string FirstName, string LastName);

public sealed record LoginUserRequest(string Email, string Password, string? TwoFactorCode = null);

public sealed record LoginUserResponse(string AccessToken, string RefreshToken);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SendEmailConfirmationRequest(string Email);

public sealed record ConfirmEmailRequest(string Email, string Token);

public sealed record ChangeEmailRequest(string NewEmail);

public sealed record ConfirmEmailChangeRequest(string NewEmail, string Token);

public sealed record ChangePhoneNumberRequest(string PhoneNumber);

public sealed record ConfirmPhoneNumberChangeRequest(string PhoneNumber, string Token);

public sealed record UpdateProfileRequest(string FirstName, string LastName);

public sealed record EnableTwoFactorRequest(string Code);

public sealed record TwoFactorSetupResponse(string SharedKey, string Email);

public sealed record TwoFactorRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

public sealed record CreateRoleRequest(string Name);

public sealed record UpdateRoleRequest(string Name);

public sealed record RoleResponse(string Id, string Name);

public sealed record AssignRolesRequest(string[] Roles);

public sealed record UpdateUserStatusRequest(bool IsActive);

public sealed record UserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// Resultado de una llamada a la API: o el valor, o los errores de validación tal como los
/// devuelve el paquete.
/// </summary>
/// <remarks>
/// El paquete responde a los fallos con un ValidationProblemDetails, cuyas claves son los
/// campos en camelCase y, para las credenciales, la cadena vacía. Modelarlo como resultado
/// —en lugar de lanzar— es lo que permite pintar el error junto a su campo.
/// </remarks>
/// <typeparam name="T">Tipo del valor devuelto cuando la llamada sale bien.</typeparam>
public sealed record ApiResult<T>
{
    private ApiResult(T? value, IReadOnlyDictionary<string, string[]>? errors, HttpStatusCode statusCode)
    {
        Value = value;
        Errors = errors ?? new Dictionary<string, string[]>();
        StatusCode = statusCode;
    }

    public T? Value { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public HttpStatusCode StatusCode { get; }

    public bool Succeeded => Errors.Count == 0;

    /// <summary>
    /// Todos los mensajes en una sola línea, para el aviso emergente.
    /// </summary>
    public string ErrorSummary => string.Join(" ", Errors.SelectMany(entry => entry.Value));

    public static ApiResult<T> Success(T? value, HttpStatusCode statusCode) => new(value, null, statusCode);

    public static ApiResult<T> Failure(IReadOnlyDictionary<string, string[]> errors, HttpStatusCode statusCode) =>
        new(default, errors, statusCode);

    public static ApiResult<T> Failure(string message, HttpStatusCode statusCode) =>
        new(default, new Dictionary<string, string[]> { [string.Empty] = [message] }, statusCode);
}

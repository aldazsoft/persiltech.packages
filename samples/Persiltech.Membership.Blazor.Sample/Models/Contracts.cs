namespace Persiltech.Membership.Blazor.Sample.Models;

// Los cuerpos que Persiltech.Membership.Blazor todavía no cubre en la 0.1.0. Se redeclaran
// aquí en lugar de referenciar el paquete de servidor: un frontend solo conoce el JSON que
// la API publica, y copiarlo es lo que pone a prueba que ese contrato baste.

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

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

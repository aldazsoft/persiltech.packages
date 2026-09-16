namespace Persiltech.Membership.Blazor.Components;

/// <summary>
/// Lo que se escribe en <c>MembershipForgotPasswordForm</c>.
/// </summary>
internal sealed class ForgotPasswordFormModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Eso no parece un correo.")]
    public string? Email { get; set; }
}

namespace Persiltech.Membership.Blazor.Components;

/// <summary>
/// Lo que se escribe en <c>MembershipResetPasswordForm</c>.
/// </summary>
/// <remarks>
/// <c>NewPassword</c> se llama así porque es la clave que devuelve la API. <c>ConfirmPassword</c>
/// no viaja a ninguna parte —el servidor solo recibe una contraseña—, pero es una propiedad del
/// modelo para que el aviso de que las dos no coinciden se pinte bajo su campo, como los demás.
/// <para>
/// El testigo no está aquí: es una credencial y no se muestra, así que no es un campo del
/// formulario. El componente lo guarda aparte.
/// </para>
/// </remarks>
internal sealed class ResetPasswordFormModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Eso no parece un correo.")]
    public string? Email { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }
}

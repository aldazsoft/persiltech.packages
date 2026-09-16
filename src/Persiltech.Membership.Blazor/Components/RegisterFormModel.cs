namespace Persiltech.Membership.Blazor.Components;

/// <summary>
/// Lo que se escribe en <c>MembershipRegisterForm</c>.
/// </summary>
/// <remarks>
/// Los nombres coinciden con las claves que devuelve la API —<c>email</c>, <c>password</c>,
/// <c>firstName</c>, <c>lastName</c>—, que es lo que hace que cada error aterrice en su campo.
/// <para>
/// No se comprueba aquí la fortaleza de la contraseña: la fija la política de Identity del
/// consumidor, que este paquete no conoce. Esa la responde el servidor, y su mensaje llega al
/// campo igual que los demás.
/// </para>
/// </remarks>
internal sealed class RegisterFormModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Eso no parece un correo.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Escribe una contraseña.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Escribe tu nombre.")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Escribe tus apellidos.")]
    public string? LastName { get; set; }
}

namespace Persiltech.Validation.Blazor.Sample.Models;

/// <summary>
/// Modelo del formulario de ejemplo.
/// </summary>
/// <remarks>
/// Las anotaciones son a propósito: demuestran que el componente <b>convive</b> con la
/// validación de cliente en lugar de sustituirla. Las reglas que el navegador puede comprobar
/// —que un campo no esté vacío— las sigue resolviendo <c>DataAnnotationsValidator</c> sin
/// preguntar a nadie; las que solo sabe el servidor —que el correo ya esté registrado— llegan
/// después y aterrizan en el mismo sitio.
/// </remarks>
public sealed class RegistrationModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Eso no parece un correo.")]
    public string? Email { get; set; }

    public string? Password { get; set; }

    /// <summary>Dirección, para ver una ruta anidada.</summary>
    public AddressModel Address { get; set; } = new();
}

/// <summary>
/// Parte anidada del modelo.
/// </summary>
/// <remarks>
/// Existe para probar lo que de verdad cuesta: que un error con la clave <c>address.city</c>
/// encuentre su campo, y no se quede sin dueño.
/// </remarks>
public sealed class AddressModel
{
    public string? City { get; set; }
}

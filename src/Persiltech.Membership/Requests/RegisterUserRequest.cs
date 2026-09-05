namespace Persiltech.Membership.Requests;

/// <summary>
/// Cuerpo de la petición de registro.
/// </summary>
/// <remarks>
/// Las cuatro propiedades son anulables y no llevan <c>required</c> a propósito: con
/// <c>required</c>, un cuerpo JSON al que le falte un campo fallaría en la deserialización
/// y el cliente recibiría un error con una forma distinta de la acordada. Siendo anulables,
/// el campo ausente llega como <see langword="null"/>, lo rechaza
/// <see cref="RequiredAttribute"/> y el error sale como
/// <c>ValidationProblemDetails</c>.
/// </remarks>
public sealed record RegisterUserRequest
{
    /// <summary>
    /// Correo de la cuenta. Es a la vez el nombre de usuario.
    /// </summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// Contraseña de la cuenta.
    /// </summary>
    /// <remarks>
    /// No lleva reglas de longitud ni de complejidad: la política de contraseñas la pone
    /// ASP.NET Core Identity, y repetirla aquí daría dos mensajes distintos para el mismo
    /// fallo.
    /// </remarks>
    [Required]
    public string? Password { get; init; }

    /// <summary>
    /// Nombre del usuario. Hasta 100 caracteres.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string? FirstName { get; init; }

    /// <summary>
    /// Apellido del usuario. Hasta 100 caracteres.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string? LastName { get; init; }
}

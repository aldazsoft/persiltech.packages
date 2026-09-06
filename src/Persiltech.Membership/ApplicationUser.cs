namespace Persiltech.Membership;

/// <summary>
/// Usuario de la aplicación. Añade el nombre propio y el apellido al usuario de
/// ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// El <see cref="IdentityUser{TKey}.UserName"/> y el <see cref="IdentityUser{TKey}.Email"/>
/// reciben ambos el correo con el que se registró la cuenta: en este paquete el correo
/// <em>es</em> el nombre de usuario.
/// <para>
/// No es <c>sealed</c>: un consumidor cuyo dominio necesite más columnas en la cuenta deriva
/// de ella y se lo dice al paquete por el parámetro de tipo de
/// <see cref="DependencyInjection.AddMembershipServices{TUser, TContext}"/>. Esas columnas
/// viven en <c>AspNetUsers</c>, pero el paquete no las valida ni las expone: son suyas.
/// </para>
/// </remarks>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Nombre del usuario. Obligatorio, hasta 100 caracteres.
    /// </summary>
    /// <remarks>
    /// Las anotaciones describen la columna que genera Entity Framework Core
    /// (<c>NOT NULL</c>, <c>nvarchar(100)</c>); no validan ninguna petición.
    /// </remarks>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario. Obligatorio, hasta 100 caracteres.
    /// </summary>
    /// <remarks>
    /// Las anotaciones describen la columna que genera Entity Framework Core
    /// (<c>NOT NULL</c>, <c>nvarchar(100)</c>); no validan ninguna petición.
    /// </remarks>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
}

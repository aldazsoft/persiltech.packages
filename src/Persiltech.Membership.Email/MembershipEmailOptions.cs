namespace Persiltech.Membership.Email;

/// <summary>
/// Marca, rutas de la aplicación cliente y origen de las plantillas.
/// </summary>
/// <remarks>
/// Es lo que hace reutilizable al paquete: dos aplicaciones con marcas y rutas distintas
/// comparten el mismo binario. Se validan al arrancar, no en el primer aviso: el registro
/// encadena <c>ValidateOnStart()</c> sobre un <see cref="IValidateOptions{TOptions}"/>
/// propio, que devuelve todos los fallos juntos.
/// </remarks>
public sealed class MembershipEmailOptions
{
    /// <summary>
    /// Nombre de la marca, que aparece en el encabezado, el pie y los asuntos. Obligatorio.
    /// </summary>
    public string BrandName { get; set; } = string.Empty;

    /// <summary>
    /// Raíz de la aplicación <em>cliente</em> —la que abre el usuario—, no la de la API.
    /// Obligatoria, y tiene que ser una URL absoluta <c>http</c> o <c>https</c>.
    /// </summary>
    /// <remarks>
    /// De ella cuelgan las tres rutas de vuelta. Una barra final sobrante no estorba: se
    /// recorta al construir el enlace.
    /// </remarks>
    public string ClientBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ruta de la pantalla que confirma el correo. Por defecto, <c>/confirm-email</c>.
    /// </summary>
    public string EmailConfirmationPath { get; set; } = "/confirm-email";

    /// <summary>
    /// Ruta de la pantalla que reinicia la contraseña. Por defecto, <c>/reset-password</c>.
    /// </summary>
    public string PasswordResetPath { get; set; } = "/reset-password";

    /// <summary>
    /// Ruta de la pantalla que confirma el cambio de correo. Por defecto,
    /// <c>/confirm-email-change</c>.
    /// </summary>
    public string EmailChangePath { get; set; } = "/confirm-email-change";

    /// <summary>
    /// Logotipo del encabezado. Opcional: sin él se rotula la marca como texto. Si se
    /// indica, tiene que ser una URL absoluta.
    /// </summary>
    /// <remarks>
    /// Los clientes de correo no resuelven rutas relativas: el logotipo tiene que estar
    /// publicado en algún sitio al que llegue quien abra el mensaje.
    /// </remarks>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Color del encabezado y del botón de acción, en hexadecimal (<c>#rgb</c> o
    /// <c>#rrggbb</c>). Por defecto, <c>#0d6efd</c>.
    /// </summary>
    public string PrimaryColor { get; set; } = "#0d6efd";

    /// <summary>
    /// Correo de contacto que se ofrece en el pie. Opcional.
    /// </summary>
    public string? SupportEmail { get; set; }

    /// <summary>
    /// Directorio en disco cuyas plantillas ganan a las embebidas, por nombre de archivo.
    /// Si se indica, tiene que existir.
    /// </summary>
    /// <remarks>
    /// Es la vía para cambiar el diseño sin bifurcar el paquete: basta con dejar ahí un
    /// archivo con el mismo nombre que el embebido. Que el directorio tenga que existir no
    /// es capricho: apuntando a uno que no está, se servirían las plantillas embebidas y
    /// nadie se enteraría de que el rebrandeo no se aplicó.
    /// <para>
    /// Las plantillas se leen una vez y se cachean, así que un cambio en disco exige
    /// reiniciar la aplicación.
    /// </para>
    /// </remarks>
    public string? TemplatesDirectory { get; set; }
}

namespace Persiltech.Membership.Blazor;

/// <summary>
/// Todo lo que se lee en los formularios del paquete.
/// </summary>
/// <remarks>
/// Es lo que los hace reutilizables fuera del español. Los rótulos estaban escritos dentro de
/// cada componente, así que una aplicación en otro idioma —o que llame «usuario» a lo que aquí
/// es «correo»— tenía que bifurcar el paquete para cambiar una palabra.
/// <para>
/// Mismo criterio que las plantillas de correo de <c>Persiltech.Membership.Email</c>: el
/// paquete trae unos valores por defecto que sirven tal cual, y el consumidor sustituye los
/// que necesite sin tocar el resto.
/// </para>
/// <para>
/// No se valida nada: un rótulo vacío sale vacío y se ve en la primera pantalla. Exigir que
/// estén todos obligaría a repetir los veinte en cada aplicación, que es justo lo contrario de
/// lo que busca esta clase.
/// </para>
/// </remarks>
public sealed class MembershipFormTexts
{
    /// <summary>Rótulo del campo de correo.</summary>
    public string Email { get; set; } = "Correo";

    /// <summary>Rótulo del campo de contraseña.</summary>
    public string Password { get; set; } = "Contraseña";

    /// <summary>Rótulo del campo del segundo factor.</summary>
    public string TwoFactorCode { get; set; } = "Código del doble factor";

    /// <summary>Rótulo del campo de nombre.</summary>
    public string FirstName { get; set; } = "Nombre";

    /// <summary>Rótulo del campo de apellidos.</summary>
    public string LastName { get; set; } = "Apellidos";

    /// <summary>Rótulo del campo de contraseña nueva.</summary>
    public string NewPassword { get; set; } = "Contraseña nueva";

    /// <summary>Rótulo del campo que repite la contraseña.</summary>
    public string ConfirmPassword { get; set; } = "Repite la contraseña";

    /// <summary>Texto de ayuda del correo bloqueado en el cambio de contraseña.</summary>
    public string LockedEmailHelp { get; set; } = "La cuenta para la que se pidió el cambio.";

    /// <summary>
    /// Descripción del botón que enseña u oculta la contraseña, para los lectores de pantalla.
    /// </summary>
    /// <remarks>
    /// Es un botón sin texto: sin esto, un lector de pantalla solo anuncia «botón».
    /// </remarks>
    public string ShowPassword { get; set; } = "Mostrar la contraseña";

    /// <summary>Lo mismo cuando la contraseña ya está a la vista.</summary>
    public string HidePassword { get; set; } = "Ocultar la contraseña";

    /// <summary>
    /// Lo que se anuncia mientras la petición está en vuelo.
    /// </summary>
    public string Working { get; set; } = "Enviando…";
}

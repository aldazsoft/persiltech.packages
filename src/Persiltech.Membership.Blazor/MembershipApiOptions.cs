namespace Persiltech.Membership.Blazor;

/// <summary>
/// Dónde está la API de <c>Persiltech.Membership</c> y en qué rutas montó sus endpoints.
/// </summary>
/// <remarks>
/// Las rutas son configurables porque en el servidor también lo son: los métodos
/// <c>Map*</c> del paquete reciben el patrón, así que un cliente que las fijara obligaría a
/// montar la API en las suyas. Los valores por defecto coinciden con los que propone el
/// paquete servidor, de modo que quien no las cambie no configura ninguna.
/// <para>
/// Lo que se puede dar por bueno lo decide <see cref="MembershipApiOptionsValidator"/>. A
/// diferencia del resto de la casa no se engancha con <c>ValidateOnStart</c>: en Blazor
/// WebAssembly no hay host que arranque servicios, así que esa validación no correría nunca.
/// Lo llama <see cref="DependencyInjection.AddMembershipBlazor"/> al registrar, que es el
/// momento más temprano que existe en este modelo de alojamiento.
/// </para>
/// </remarks>
public sealed class MembershipApiOptions
{
    /// <summary>
    /// Raíz de la API, con esquema y autoridad. Obligatoria.
    /// </summary>
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>Ruta del endpoint de autenticación.</summary>
    public string LoginPath { get; set; } = "user/login";

    /// <summary>Ruta del endpoint de registro.</summary>
    public string RegisterPath { get; set; } = "user/register";

    /// <summary>Ruta del endpoint de renovación de la sesión.</summary>
    public string RefreshPath { get; set; } = "user/refresh";

    /// <summary>Ruta del endpoint de cierre de sesión.</summary>
    public string LogoutPath { get; set; } = "user/logout";

    /// <summary>Patrón base del grupo de contraseñas.</summary>
    public string PasswordPath { get; set; } = "password";

    /// <summary>Patrón base del grupo de correo.</summary>
    public string EmailPath { get; set; } = "email";

    /// <summary>Patrón base del grupo de teléfono.</summary>
    public string PhonePath { get; set; } = "phone";

    /// <summary>Patrón base del grupo de perfil.</summary>
    public string ProfilePath { get; set; } = "profile";

    /// <summary>Patrón base del grupo de doble factor.</summary>
    public string TwoFactorPath { get; set; } = "twofactor";

    /// <summary>Patrón base del grupo de roles.</summary>
    public string RolesPath { get; set; } = "roles";

    /// <summary>Patrón base del grupo de usuarios.</summary>
    public string UsersPath { get; set; } = "users";
}

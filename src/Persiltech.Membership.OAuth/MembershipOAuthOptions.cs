namespace Persiltech.Membership.OAuth;

/// <summary>
/// Opciones del servidor de autorización. El consumidor las rellena con el delegado
/// <c>Action&lt;MembershipOAuthOptions&gt;</c> de
/// <see cref="DependencyInjection.AddMembershipOAuthServer"/>.
/// </summary>
public sealed class MembershipOAuthOptions
{
    /// <summary>
    /// Ruta del endpoint de autorización, donde empieza el flujo Authorization Code.
    /// </summary>
    [Required]
    public string AuthorizationEndpointPath { get; set; } = "/connect/authorize";

    /// <summary>
    /// Ruta del endpoint de testigos, donde se canjea el código y se renueva la sesión.
    /// </summary>
    [Required]
    public string TokenEndpointPath { get; set; } = "/connect/token";

    /// <summary>
    /// Ruta del endpoint de información del usuario, que devuelve sus reclamaciones a
    /// partir del token de acceso.
    /// </summary>
    [Required]
    public string UserInfoEndpointPath { get; set; } = "/connect/userinfo";

    /// <summary>
    /// Ruta del endpoint de fin de sesión, que cierra la sesión interactiva.
    /// </summary>
    [Required]
    public string EndSessionEndpointPath { get; set; } = "/connect/logout";

    /// <summary>
    /// Ruta del endpoint de revocación, que anula un testigo antes de su caducidad.
    /// </summary>
    /// <remarks>
    /// Lo atiende OpenIddict por completo: no hay manejador propio que montar, y por eso
    /// esta ruta no aparece en <see cref="OAuthEndpoints.MapMembershipOAuthEndpoints"/>.
    /// </remarks>
    [Required]
    public string RevocationEndpointPath { get; set; } = "/connect/revoke";

    /// <summary>
    /// Ruta a la que se envía al usuario cuando llega al endpoint de autorización sin
    /// haber iniciado sesión.
    /// </summary>
    /// <remarks>
    /// La pantalla de inicio de sesión es del consumidor: el paquete no trae ninguna, y no
    /// podría, porque el flujo Authorization Code exige una sesión interactiva de navegador
    /// y este paquete no impone ni interfaz ni maquetación.
    /// </remarks>
    [Required]
    public string LoginPath { get; set; } = "/account/login";

    /// <summary>
    /// Esquema de autenticación con el que se comprueba la sesión interactiva en el
    /// endpoint de autorización.
    /// </summary>
    /// <remarks>
    /// Por defecto el de cookies. No es el esquema con el que se validan los tokens que
    /// emite el servidor: ese es del consumidor, igual que en el paquete base.
    /// </remarks>
    [Required]
    public string InteractiveAuthenticationScheme { get; set; } =
        CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>
    /// Minutos de vigencia del token de acceso desde su emisión.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AccessTokenLifetimeInMinutes { get; set; } = 30;

    /// <summary>
    /// Días de vigencia del testigo de renovación desde su emisión.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int RefreshTokenLifetimeInDays { get; set; } = 14;

    /// <summary>
    /// Ámbitos que el servidor reconoce, además de los estándar de OpenID Connect.
    /// </summary>
    public string[] Scopes { get; set; } = [];

    /// <summary>
    /// Usa los certificados de desarrollo que genera OpenIddict en lugar de los del
    /// consumidor.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> solo vale para desarrollo: los certificados se generan en el
    /// almacén del usuario y no sirven en producción ni entre varias instancias. En
    /// producción hay que registrar certificados propios con
    /// <c>AddSigningCertificate</c> y <c>AddEncryptionCertificate</c> sobre el constructor
    /// que expone <see cref="DependencyInjection.AddMembershipOAuthServer"/>.
    /// </remarks>
    public bool UseDevelopmentCertificates { get; set; }
}

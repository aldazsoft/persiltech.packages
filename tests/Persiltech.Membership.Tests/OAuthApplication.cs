namespace Persiltech.Membership.Tests;

/// <summary>
/// Levanta el servidor de autorización de verdad —OpenIddict, sus tablas y los endpoints—
/// sobre un host de prueba y SQLite en memoria.
/// </summary>
/// <remarks>
/// Las pruebas que ya existían sobre OAuth solo inspeccionaban metadatos de las rutas: qué
/// se monta, con qué verbos y con qué autorización. Este harness existe para lo otro, que
/// es lo que de verdad importa en un servidor de autorización: que los flujos emitan y
/// rechacen testigos como deben.
/// <para>
/// El esquema interactivo es una cookie, igual que en un consumidor real: el paquete no
/// monta ninguno a propósito, porque la pantalla de inicio de sesión es del consumidor.
/// Para poder ejercer el flujo de código de autorización sin una pantalla, el harness
/// expone <c>/test/signin</c>, que abre esa sesión de navegador.
/// </para>
/// </remarks>
internal sealed class OAuthApplication : IAsyncDisposable
{
    internal const string PublicClientId = "browser-app";
    internal const string ConfidentialClientId = "service-app";
    internal const string ConfidentialClientSecret = "un-secreto-de-cliente-para-pruebas";
    internal const string RedirectUri = "https://cliente.persiltech.test/callback";

    private const string SecurityKey = "una-clave-de-firma-de-32-caracteres";
    private const string InteractiveScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    private readonly SqliteConnection MembershipConnection;
    private readonly SqliteConnection OAuthConnection;
    private readonly WebApplication Application;

    private OAuthApplication(
        SqliteConnection membershipConnection,
        SqliteConnection oauthConnection,
        WebApplication application)
    {
        MembershipConnection = membershipConnection;
        OAuthConnection = oauthConnection;
        Application = application;
    }

    /// <summary>
    /// Cliente HTTP contra el host de prueba, sin seguir las redirecciones: el flujo de
    /// autorización se comprueba leyendo la respuesta 302 y su cabecera Location.
    /// </summary>
    internal HttpClient Client => Application.GetTestClient();

    /// <summary>
    /// Proveedor de servicios de la aplicación.
    /// </summary>
    internal IServiceProvider Services => Application.Services;

    /// <summary>
    /// Arranca el servidor con las tablas creadas y los dos clientes registrados.
    /// </summary>
    /// <returns>El servidor listo para recibir peticiones.</returns>
    internal static async Task<OAuthApplication> StartAsync()
    {
        // Una conexión por contexto: EnsureCreated es todo-o-nada por base de datos, así
        // que compartir una dejaría al segundo contexto sin sus tablas al ver que la base
        // ya existe. Los dos contextos son independientes y no comparten ninguna tabla.
        var membershipConnection = new SqliteConnection("DataSource=:memory:");
        await membershipConnection.OpenAsync();

        var oauthConnection = new SqliteConnection("DataSource=:memory:");
        await oauthConnection.OpenAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddMembershipServices(
            jwt =>
            {
                jwt.SecurityKey = SecurityKey;
                jwt.ValidIssuer = "https://oauth.persiltech.test";
                jwt.ValidAudience = "persiltech-tests";
                jwt.ExpireInMinutes = 30;
            },
            options => options.UseSqlite(membershipConnection));

        builder.Services.AddMembershipOAuthServer(
            options => options.UseSqlite(oauthConnection),
            options =>
            {
                options.InteractiveAuthenticationScheme = InteractiveScheme;
                // Los certificados de desarrollo evitan tener que fabricar uno propio; el
                // cifrado del token de acceso ya viene desactivado por el paquete.
                options.UseDevelopmentCertificates = true;
            },
            // El host de prueba habla HTTP y OpenIddict exige HTTPS. Se relaja aquí, por el
            // punto de extensión que el paquete expone justo para lo que no decide: en
            // producción la exigencia sigue en pie, que es lo correcto.
            server => server.UseAspNetCore().DisableTransportSecurityRequirement());

        builder.Services.AddAuthentication(InteractiveScheme).AddCookie(InteractiveScheme);
        builder.Services.AddAuthorization();

        var application = builder.Build();

        application.UseAuthentication();
        application.UseAuthorization();

        application.MapMembershipOAuthEndpoints();

        // Sustituye a la pantalla de inicio de sesión del consumidor: abre la sesión de
        // navegador que el endpoint de autorización espera encontrar.
        application.MapGet("/test/signin/{email}", async (string email, HttpContext context, UserManager<ApplicationUser> users) =>
        {
            var user = await users.FindByEmailAsync(email);

            if (user is null)
            {
                return Results.NotFound();
            }

            var identity = new ClaimsIdentity(InteractiveScheme);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.Email!));

            await context.SignInAsync(InteractiveScheme, new ClaimsPrincipal(identity));

            return Results.Ok();
        });

        using (var scope = application.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<MembershipOAuthDbContext>().Database.EnsureCreatedAsync();
        }

        await application.StartAsync();

        await application.Services.RegisterMembershipOAuthClientsAsync(
        [
            new MembershipOAuthClient(
                PublicClientId,
                "Aplicación de navegador",
                [RedirectUri],
                [Scopes.OpenId, Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess]),
            new MembershipOAuthClient(
                ConfidentialClientId,
                "Servicio de confianza",
                [],
                [Scopes.Email],
                ConfidentialClientSecret)
        ]);

        return new OAuthApplication(membershipConnection, oauthConnection, application);
    }

    /// <summary>
    /// Registra una cuenta por el endpoint del paquete base.
    /// </summary>
    /// <param name="email">Correo de la cuenta.</param>
    /// <returns>El identificador de la cuenta creada.</returns>
    internal async Task<string> RegisterAsync(string email = "juan.perez@example.com")
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Juan",
            LastName = "Pérez"
        };

        var created = await users.CreateAsync(user, "Passw0rd!");

        Assert.True(created.Succeeded);

        return user.Id;
    }

    /// <summary>
    /// Abre la sesión de navegador de una cuenta y devuelve un cliente que la conserva.
    /// </summary>
    /// <remarks>
    /// El cliente del host de prueba no lleva contenedor de cookies, así que la de sesión
    /// se copia a mano de la respuesta a la cabecera de las peticiones siguientes. Sin esto
    /// el endpoint de autorización no vería la sesión y devolvería al inicio de sesión.
    /// </remarks>
    /// <param name="email">Correo de la cuenta.</param>
    /// <returns>El cliente con la sesión abierta.</returns>
    internal async Task<HttpClient> SignInAsync(string email = "juan.perez@example.com")
    {
        var client = Client;
        var response = await client.GetAsync($"/test/signin/{email}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));

        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);

        return client;
    }

    /// <summary>
    /// Envía una petición al endpoint de testigos.
    /// </summary>
    /// <param name="form">Parámetros de la concesión.</param>
    /// <returns>La respuesta tal cual, para poder comprobar también los rechazos.</returns>
    internal Task<HttpResponseMessage> RequestTokenAsync(Dictionary<string, string> form) =>
        Client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(form),
            TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Application.StopAsync();
        await Application.DisposeAsync();
        await MembershipConnection.DisposeAsync();
        await OAuthConnection.DisposeAsync();
    }
}

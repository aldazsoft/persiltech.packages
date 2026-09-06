namespace Persiltech.Membership.Tests;

/// <summary>
/// Levanta la aplicación de verdad —contenedor, endpoints y base de datos— sobre un host
/// de prueba y SQLite en memoria.
/// </summary>
/// <remarks>
/// Compone el paquete como lo haría un consumidor, en lugar de referenciar el proyecto de
/// ejemplo: lo que se verifica es el paquete, no el cableado del sample. Es la única forma
/// de destapar un servicio sin registrar o un testigo que no se puede generar, que es
/// justo lo que las pruebas de metadatos no ven.
/// </remarks>
internal sealed class MembershipApplication : IAsyncDisposable
{
    private const string SecurityKey = "una-clave-de-firma-de-32-caracteres";
    private const string ValidIssuer = "https://membership.persiltech.test";
    private const string ValidAudience = "persiltech-tests";

    private readonly SqliteConnection Connection;
    private readonly WebApplication Application;

    private MembershipApplication(SqliteConnection connection, WebApplication application)
    {
        Connection = connection;
        Application = application;
        Messages = application.Services.GetRequiredService<RecordingMessageSender>();
    }

    /// <summary>
    /// Avisos que el paquete entregó por sus puertos de salida durante la prueba.
    /// </summary>
    internal RecordingMessageSender Messages { get; }

    /// <summary>
    /// Cliente HTTP contra el host de prueba.
    /// </summary>
    internal HttpClient Client => Application.GetTestClient();

    /// <summary>
    /// Proveedor de servicios de la aplicación, para lo que se invoca fuera de una petición.
    /// </summary>
    internal IServiceProvider Services => Application.Services;

    /// <summary>
    /// Arranca la aplicación con el esquema ya creado.
    /// </summary>
    /// <param name="configureIdentity">Ajustes de Identity para la prueba.</param>
    /// <param name="settings">
    /// Valores de configuración de la aplicación, para las pruebas que enlazan opciones
    /// desde la configuración en lugar de fijarlas con un delegado.
    /// </param>
    /// <returns>La aplicación lista para recibir peticiones.</returns>
    internal static async Task<MembershipApplication> StartAsync(
        Action<IdentityOptions>? configureIdentity = null,
        IEnumerable<KeyValuePair<string, string?>>? settings = null)
    {
        // La conexión se mantiene abierta a propósito: SQLite descarta la base en memoria
        // en cuanto se cierra la última.
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        if (settings is not null)
        {
            builder.Configuration.AddInMemoryCollection(settings);
        }

        builder.Services.AddMembershipServices(
            jwt =>
            {
                jwt.SecurityKey = SecurityKey;
                jwt.ValidIssuer = ValidIssuer;
                jwt.ValidAudience = ValidAudience;
                jwt.ExpireInMinutes = 30;
            },
            options => options.UseSqlite(connection));

        if (configureIdentity is not null)
        {
            builder.Services.Configure(configureIdentity);
        }

        // El cableado que haría un consumidor para gobernar Identity desde appsettings.
        // El paquete no participa: IdentityOptions ya es una clase de opciones, así que se
        // enlaza con el sistema de configuración de siempre.
        builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"));

        builder.Services.AddSingleton<RecordingMessageSender>();
        builder.Services.AddSingleton<IMembershipEmailSender>(
            provider => provider.GetRequiredService<RecordingMessageSender>());
        builder.Services.AddSingleton<IMembershipSmsSender>(
            provider => provider.GetRequiredService<RecordingMessageSender>());

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = ValidIssuer,
                ValidAudience = ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey))
            });

        builder.Services.AddAuthorization();

        var application = builder.Build();

        application.UseAuthentication();
        application.UseAuthorization();

        application.MapMembershipEndpoints();
        application.MapPasswordEndpoints();
        application.MapEmailEndpoints();
        application.MapPhoneNumberEndpoints();
        application.MapProfileEndpoints();
        application.MapTwoFactorEndpoints();

        var administration = application.MapGroup(string.Empty).RequireAuthorization();
        administration.MapRoleEndpoints();
        administration.MapUserEndpoints();

        using (var scope = application.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();

            // En una prueba el esquema se crea de una vez: no hay historial que versionar,
            // y las migraciones son del consumidor, no del paquete.
            await context.Database.EnsureCreatedAsync();
        }

        await application.StartAsync();

        return new MembershipApplication(connection, application);
    }

    /// <summary>
    /// Registra una cuenta y devuelve su token de acceso.
    /// </summary>
    /// <param name="email">Correo de la cuenta.</param>
    /// <param name="password">Contraseña de la cuenta.</param>
    /// <returns>El token de acceso recién emitido.</returns>
    internal async Task<string> RegisterAndLoginAsync(
        string email = "juan.perez@example.com",
        string password = "Passw0rd!")
    {
        var registered = await Client.PostAsJsonAsync(
            "user/register",
            new { email, password, firstName = "Juan", lastName = "Pérez" });

        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        return await LoginAsync(email, password);
    }

    /// <summary>
    /// Autentica una cuenta y devuelve su token de acceso.
    /// </summary>
    /// <param name="email">Correo de la cuenta.</param>
    /// <param name="password">Contraseña de la cuenta.</param>
    /// <param name="twoFactorCode">Segundo factor, si la cuenta lo tiene activado.</param>
    /// <returns>El token de acceso recién emitido.</returns>
    internal async Task<string> LoginAsync(string email, string password, string? twoFactorCode = null)
    {
        var response = await Client.PostAsJsonAsync(
            "user/login",
            new { email, password, twoFactorCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return body.GetProperty("accessToken").GetString()!;
    }

    /// <summary>
    /// Devuelve un cliente que envía el token en cada petición.
    /// </summary>
    /// <param name="accessToken">Token de acceso.</param>
    /// <returns>El cliente autenticado.</returns>
    internal HttpClient AuthenticatedClient(string accessToken)
    {
        var client = Application.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Application.StopAsync();
        await Application.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

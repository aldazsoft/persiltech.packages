namespace Persiltech.Membership.Tests;

/// <summary>
/// Usuario del consumidor: <see cref="ApplicationUser"/> con las columnas de su dominio.
/// </summary>
public sealed class ExtendedUser : ApplicationUser
{
    /// <summary>Número del documento de identidad.</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Fecha de nacimiento.</summary>
    public DateOnly? BirthDate { get; set; }
}

/// <summary>
/// Contexto del consumidor sobre su propio usuario.
/// </summary>
/// <param name="options">Opciones del contexto.</param>
public sealed class ExtendedDbContext(DbContextOptions<ExtendedDbContext> options)
    : MembershipDbContext<ExtendedUser>(options);

/// <summary>
/// Verifica que el paquete se compone igual con un usuario derivado, y que las columnas del
/// consumidor viajan en la misma cuenta.
/// </summary>
public sealed class ExtendedUserTests
{
    private const string SecurityKey = "una-clave-de-firma-de-32-caracteres";

    [Fact]
    public async Task ElPaqueteSeComponeConUnUsuarioDerivado()
    {
        await using var application = await StartAsync();

        var registered = await application.Client.PostAsJsonAsync(
            "user/register",
            new { email = "ada@example.com", password = "Passw0rd!", firstName = "Ada", lastName = "Lovelace" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var login = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "ada@example.com", password = "Passw0rd!" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = (await login.Content.ReadFromJsonAsync<LoginUserResponse>(
            TestContext.Current.CancellationToken))!;

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task LaRenovacionFuncionaSobreElUsuarioDerivado()
    {
        await using var application = await StartAsync();

        await application.Client.PostAsJsonAsync(
            "user/register",
            new { email = "ada@example.com", password = "Passw0rd!", firstName = "Ada", lastName = "Lovelace" },
            TestContext.Current.CancellationToken);

        var login = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "ada@example.com", password = "Passw0rd!" },
            TestContext.Current.CancellationToken);

        var tokens = (await login.Content.ReadFromJsonAsync<LoginUserResponse>(
            TestContext.Current.CancellationToken))!;

        var refreshed = await application.Client.PostAsJsonAsync(
            "user/refresh",
            new { refreshToken = tokens.RefreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
    }

    [Fact]
    public async Task LasColumnasDelConsumidorViajanEnLaMismaCuenta()
    {
        await using var application = await StartAsync();

        await application.Client.PostAsJsonAsync(
            "user/register",
            new { email = "ada@example.com", password = "Passw0rd!", firstName = "Ada", lastName = "Lovelace" },
            TestContext.Current.CancellationToken);

        using var scope = application.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ExtendedUser>>();

        var user = await userManager.FindByEmailAsync("ada@example.com");

        Assert.NotNull(user);

        user.DocumentNumber = "45678912";
        user.BirthDate = new DateOnly(1990, 5, 17);

        Assert.True((await userManager.UpdateAsync(user)).Succeeded);

        // Se relee desde la base para comprobar que se persistieron, no que siguen en memoria.
        var context = scope.ServiceProvider.GetRequiredService<ExtendedDbContext>();
        var stored = await context.Users.AsNoTracking()
            .SingleAsync(u => u.Email == "ada@example.com", TestContext.Current.CancellationToken);

        Assert.Equal("45678912", stored.DocumentNumber);
        Assert.Equal(new DateOnly(1990, 5, 17), stored.BirthDate);

        // Y en AspNetUsers, no en una tabla aparte: es la misma cuenta.
        Assert.Equal(
            "AspNetUsers",
            context.Model.FindEntityType(typeof(ExtendedUser))!.GetTableName());
    }

    private static async Task<ExtendedApplication> StartAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddMembershipServices<ExtendedUser, ExtendedDbContext>(
            jwt =>
            {
                jwt.SecurityKey = SecurityKey;
                jwt.ValidIssuer = "https://membership.persiltech.test";
                jwt.ValidAudience = "persiltech-tests";
                jwt.ExpireInMinutes = 30;
            },
            options => options.UseSqlite(connection));

        // Lo que compone el consumidor: el paquete emite el token pero no lo valida. Además
        // es de aquí de donde sale la protección de datos que los proveedores de testigos de
        // Identity necesitan.
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://membership.persiltech.test",
                ValidAudience = "persiltech-tests",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey))
            });

        builder.Services.AddAuthorization();

        var application = builder.Build();

        application.UseAuthentication();
        application.UseAuthorization();

        application.MapMembershipEndpoints<ExtendedUser>();
        application.MapSessionEndpoints<ExtendedUser>();

        using (var scope = application.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ExtendedDbContext>()
                .Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await application.StartAsync(TestContext.Current.CancellationToken);

        return new ExtendedApplication(connection, application);
    }

    private sealed class ExtendedApplication(SqliteConnection connection, WebApplication application)
        : IAsyncDisposable
    {
        internal HttpClient Client => application.GetTestClient();

        internal IServiceProvider Services => application.Services;

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync();
            await application.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

namespace Persiltech.Membership.Tests;

/// <summary>
/// El aviso por correo de que una cuenta acaba de quedar bloqueada.
/// </summary>
/// <remarks>
/// Existe porque la respuesta del inicio de sesión <b>no</b> distingue «contraseña incorrecta»
/// de «cuenta bloqueada», y no debe hacerlo: distinguirlo confirmaría a quien prueba correos
/// cuáles tienen cuenta. El correo lleva esa información solo a quien controla el buzón.
/// <para>
/// Todas las pruebas bajan el tope a tres intentos: con el de por defecto harían falta más
/// peticiones para decir lo mismo.
/// </para>
/// </remarks>
public class AccountLockoutNoticeTests
{
    private const string Email = "juan.perez@example.com";
    private const string Password = "Passw0rd!";
    private const string WrongPassword = "no-es-esta";

    [Fact]
    public async Task TheNoticeGoesOutWhenTheLockoutTriggers()
    {
        await using var application = await StartWithLockoutAfter(3);

        await application.RegisterAndLoginAsync(Email, Password);

        await FailLoginAsync(application, times: 3);

        var notice = Assert.Single(application.Messages.Lockouts);

        Assert.Equal(Email, notice.Email);
        Assert.Equal("Juan", notice.FirstName);
        Assert.Equal("Pérez", notice.LastName);
    }

    /// <summary>
    /// Un intento fallido que no llega a bloquear no avisa de nada.
    /// </summary>
    /// <remarks>
    /// Si avisara, cada tecleo equivocado mandaría un correo y la gente dejaría de leerlos,
    /// que es la forma más segura de que el aviso importante pase desapercibido.
    /// </remarks>
    [Fact]
    public async Task TheNoticeDoesNotGoOutBeforeTheLockout()
    {
        await using var application = await StartWithLockoutAfter(3);

        await application.RegisterAndLoginAsync(Email, Password);

        await FailLoginAsync(application, times: 2);

        Assert.Empty(application.Messages.Lockouts);
    }

    /// <summary>
    /// Y sale una sola vez, no en cada intento posterior.
    /// </summary>
    /// <remarks>
    /// Lo garantiza el orden del endpoint: el bloqueo se comprueba antes que la contraseña, así
    /// que los intentos que llegan con la cuenta ya bloqueada se cortan antes de contar nada.
    /// </remarks>
    [Fact]
    public async Task TheNoticeGoesOutOnlyOncePerLockout()
    {
        await using var application = await StartWithLockoutAfter(3);

        await application.RegisterAndLoginAsync(Email, Password);

        await FailLoginAsync(application, times: 8);

        Assert.Single(application.Messages.Lockouts);
    }

    [Fact]
    public async Task TheNoticeSaysHowLongTheLockoutLasts()
    {
        await using var application = await StartWithLockoutAfter(3, lockoutMinutes: 15);

        await application.RegisterAndLoginAsync(Email, Password);

        await FailLoginAsync(application, times: 3);

        var notice = Assert.Single(application.Messages.Lockouts);

        // Redondeado hacia arriba, así que los quince minutos siguen siendo quince aunque la
        // prueba tarde unos milisegundos en llegar hasta aquí.
        Assert.Equal(15, notice.LockoutMinutes);
    }

    /// <summary>
    /// El aviso no lleva testigo alguno.
    /// </summary>
    /// <remarks>
    /// Es la propiedad que lo hace seguro: con un enlace de reinicio dentro, bastaría con
    /// fallar la contraseña de alguien para que le llegara al buzón un testigo válido que no ha
    /// pedido. Se comprueba sobre el tipo, que es donde se decide.
    /// </remarks>
    [Fact]
    public void TheNoticeCarriesNoToken()
    {
        var properties = typeof(AccountLockedMessage)
            .GetProperties()
            .Select(property => property.Name);

        Assert.DoesNotContain("Token", properties, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TheNoticeCarriesThePortalThatWasUsed()
    {
        await using var application = await StartWithLockoutAfter(3);

        await application.RegisterAndLoginAsync(Email, Password);

        await FailLoginAsync(application, times: 3, clientKey: "MegadBlazorCustomer");

        Assert.Equal("MegadBlazorCustomer", Assert.Single(application.Messages.Lockouts).ClientKey);
    }

    /// <summary>
    /// Un servidor de correo caído no puede dejar sin autenticar a nadie.
    /// </summary>
    /// <remarks>
    /// El aviso es informativo: si su envío revienta, el inicio de sesión responde lo mismo que
    /// habría respondido sin él. Lo contrario convertiría una avería del correo en una caída de
    /// la autenticación.
    /// </remarks>
    [Fact]
    public async Task AFailedNoticeDoesNotBreakTheLogin()
    {
        await using var application = await StartWithLockoutAfter(3);

        await application.RegisterAndLoginAsync(Email, Password);

        application.Messages.FailAccountLocked = true;

        var response = await PostLoginAsync(application, WrongPassword);
        response = await PostLoginAsync(application, WrongPassword);
        response = await PostLoginAsync(application, WrongPassword);

        // El tercero es el que dispara el bloqueo y, con él, el envío que falla.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Y quien no registre un emisor de correo sigue pudiendo autenticarse.
    /// </summary>
    /// <remarks>
    /// El puerto es opcional en este endpoint, al contrario que en los de contraseña o correo,
    /// donde el envío es el objeto de la petición.
    /// </remarks>
    [Fact]
    public async Task TheLoginWorksWithoutAnEmailSender()
    {
        await using var application = await MembershipApplication.StartAsync(
            configureIdentity: identity =>
            {
                identity.Lockout.MaxFailedAccessAttempts = 3;
                identity.Lockout.AllowedForNewUsers = true;
            },
            registerMessageSender: false);

        await CreateUserAsync(application);

        var response = await PostLoginAsync(application, WrongPassword);

        // Antes que el bloqueo: si el endpoint no supiera resolver un emisor ausente, esto
        // sería un 500 y la cuenta nunca llegaría a contar el fallo.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await FailLoginAsync(application, times: 2);

        // Sin emisor registrado, el bloqueo ocurre igual.
        Assert.True(await IsLockedOutAsync(application));
    }

    private static async Task CreateUserAsync(MembershipApplication application)
    {
        using var scope = application.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = Email,
            Email = Email,
            FirstName = "Juan",
            LastName = "Pérez"
        };

        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
    }

    /// <summary>
    /// Lee el bloqueo en un ámbito recién creado.
    /// </summary>
    /// <remarks>
    /// Hace falta uno nuevo: el contexto del ámbito que creó la cuenta sigue con esa entidad
    /// en su rastreador, y devolvería los valores de antes de que las peticiones HTTP —cada
    /// una en su propio ámbito— contaran los fallos.
    /// </remarks>
    private static async Task<bool> IsLockedOutAsync(MembershipApplication application)
    {
        using var scope = application.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(Email);

        return user is not null && await userManager.IsLockedOutAsync(user);
    }

    private static Task<MembershipApplication> StartWithLockoutAfter(
        int attempts,
        int lockoutMinutes = 15) =>
        MembershipApplication.StartAsync(configureIdentity: identity =>
        {
            identity.Lockout.MaxFailedAccessAttempts = attempts;
            identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutMinutes);
            identity.Lockout.AllowedForNewUsers = true;
        });

    private static async Task FailLoginAsync(
        MembershipApplication application,
        int times,
        string? clientKey = null)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            await PostLoginAsync(application, WrongPassword, clientKey);
        }
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(
        MembershipApplication application,
        string password,
        string? clientKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "user/login")
        {
            Content = JsonContent.Create(new { email = Email, password })
        };

        if (clientKey is not null)
        {
            request.Headers.Add(MembershipHeaders.ClientId, clientKey);
        }

        return await application.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}

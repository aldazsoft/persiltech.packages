namespace Persiltech.Membership.Tests;

/// <summary>
/// Recorre los flujos completos contra una aplicación real y una base de datos real.
/// </summary>
public class MembershipIntegrationTests
{
    [Fact]
    public async Task RegisteringAndAuthenticatingIssuesAUsableToken()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();

        var response = await application.AuthenticatedClient(accessToken).GetAsync("users/current", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal("juan.perez@example.com", user.GetProperty("email").GetString());
        Assert.True(user.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task TheWrongPasswordIsRejected()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        var response = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "juan.perez@example.com", password = "otra-cosa" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheEmailConfirmationFlowRunsEndToEnd()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        var requested = await application.Client.PostAsJsonAsync(
            "email/confirmation/send",
            new { email = "juan.perez@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, requested.StatusCode);

        var message = Assert.Single(application.Messages.Confirmations);

        var confirmed = await application.Client.PostAsJsonAsync(
            "email/confirmation",
            new { email = message.Email, token = message.Token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
    }

    [Fact]
    public async Task AnUnknownEmailGetsTheSameAnswerAndSendsNothing()
    {
        await using var application = await MembershipApplication.StartAsync();

        var response = await application.Client.PostAsJsonAsync(
            "password/forgot",
            new { email = "no-existe@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(application.Messages.Resets);
    }

    [Fact]
    public async Task ThePasswordResetFlowRunsEndToEndAndTheNewPasswordWorks()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        var requested = await application.Client.PostAsJsonAsync(
            "password/forgot",
            new { email = "juan.perez@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, requested.StatusCode);

        var message = Assert.Single(application.Messages.Resets);

        var reset = await application.Client.PostAsJsonAsync(
            "password/reset",
            new { email = message.Email, token = message.Token, newPassword = "OtraPassw0rd!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        await application.LoginAsync("juan.perez@example.com", "OtraPassw0rd!");
    }

    [Fact]
    public async Task TheEmailChangeMovesTheUserNameToo()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();
        var client = application.AuthenticatedClient(accessToken);

        var requested = await client.PostAsJsonAsync(
            "email/change",
            new { newEmail = "juan.nuevo@example.com" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, requested.StatusCode);

        var message = Assert.Single(application.Messages.Changes);

        var confirmed = await client.PostAsJsonAsync(
            "email/change/confirm",
            new { newEmail = message.NewEmail, token = message.Token }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);

        await application.LoginAsync("juan.nuevo@example.com", "Passw0rd!");
    }

    [Fact]
    public async Task ARoleAssignedToAUserTravelsInTheNextToken()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();
        var client = application.AuthenticatedClient(accessToken);

        var created = await client.PostAsJsonAsync("roles", new { name = "Administrators" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var current = await client.GetFromJsonAsync<JsonElement>("users/current", TestContext.Current.CancellationToken);
        var userId = current.GetProperty("id").GetString();

        var assigned = await client.PutAsJsonAsync(
            $"users/{userId}/roles",
            new { roles = new[] { "Administrators" } }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);

        var renewed = await application.LoginAsync("juan.perez@example.com", "Passw0rd!");
        var token = new JsonWebTokenHandler().ReadJsonWebToken(renewed);

        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.Role && claim.Value == "Administrators");
    }

    [Fact]
    public async Task AssigningAnUnknownRoleChangesNothing()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();
        var client = application.AuthenticatedClient(accessToken);

        var current = await client.GetFromJsonAsync<JsonElement>("users/current", TestContext.Current.CancellationToken);
        var userId = current.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync(
            $"users/{userId}/roles",
            new { roles = new[] { "NoExiste" } }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ADeactivatedAccountCannotAuthenticate()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();
        var client = application.AuthenticatedClient(accessToken);

        var current = await client.GetFromJsonAsync<JsonElement>("users/current", TestContext.Current.CancellationToken);
        var userId = current.GetProperty("id").GetString();

        var deactivated = await client.PutAsJsonAsync($"users/{userId}/status", new { isActive = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);

        var response = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "juan.perez@example.com", password = "Passw0rd!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheProfileCanBeUpdatedAndTheAccountDeleted()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();
        var client = application.AuthenticatedClient(accessToken);

        var updated = await client.PutAsJsonAsync(
            "profile",
            new { firstName = "Juan Carlos", lastName = "Pérez" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var profile = await updated.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.Equal("Juan Carlos", profile.GetProperty("firstName").GetString());

        var deleted = await client.DeleteAsync("profile", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var response = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "juan.perez@example.com", password = "Passw0rd!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheTwoFactorSetupReturnsAUsableSharedKey()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();

        var response = await application.AuthenticatedClient(accessToken)
            .PostAsync("twofactor/setup", content: null, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setup = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(setup.GetProperty("sharedKey").GetString()));
        Assert.Equal("juan.perez@example.com", setup.GetProperty("email").GetString());
    }

    [Fact]
    public async Task RepeatedFailuresLockTheAccountOut()
    {
        await using var application = await MembershipApplication.StartAsync(identity =>
        {
            identity.Lockout.MaxFailedAccessAttempts = 2;
            identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        });

        await application.RegisterAndLoginAsync();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await application.Client.PostAsJsonAsync(
                "user/login",
                new { email = "juan.perez@example.com", password = "otra-cosa" }, TestContext.Current.CancellationToken);
        }

        var response = await application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "juan.perez@example.com", password = "Passw0rd!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheAdministratorSeedIsIdempotent()
    {
        await using var application = await MembershipApplication.StartAsync();

        var administrator = new MembershipAdministrator(
            "admin@example.com",
            "Passw0rd!",
            "Ada",
            "Lovelace");

        Assert.True(await application.Services.SeedMembershipAdministratorAsync(administrator, TestContext.Current.CancellationToken));
        Assert.False(await application.Services.SeedMembershipAdministratorAsync(administrator, TestContext.Current.CancellationToken));

        var accessToken = await application.LoginAsync("admin@example.com", "Passw0rd!");
        var token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

        Assert.Contains(
            token.Claims,
            claim => claim.Type == ClaimTypes.Role && claim.Value == "Administrator");
    }

    [Fact]
    public async Task TheAdministrationEndpointsRejectAnAnonymousCaller()
    {
        await using var application = await MembershipApplication.StartAsync();

        var response = await application.Client.GetAsync("roles", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnUnconfirmedEmailAuthenticatesWhenIdentityDoesNotRequireConfirmation()
    {
        await using var application = await MembershipApplication.StartAsync();

        await RegisterAsync(application);

        Assert.False(await IsEmailConfirmedAsync(application));

        var response = await LoginAsync(application);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task AnUnconfirmedEmailIsRejectedWhenIdentityRequiresConfirmation()
    {
        await using var application = await MembershipApplication.StartAsync(
            identity => identity.SignIn.RequireConfirmedEmail = true);

        await RegisterAsync(application);

        Assert.False(await IsEmailConfirmedAsync(application));

        var response = await LoginAsync(application);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // IdentityOptions ya es una clase de opciones, asi que exigir el correo confirmado no
    // necesita ninguna opcion propia del paquete: se gobierna desde appsettings con el
    // enlace de configuracion de siempre, igual que la politica de contrasenas o el bloqueo.
    [Fact]
    public async Task RequiringAConfirmedEmailCanComeFromConfiguration()
    {
        await using var application = await MembershipApplication.StartAsync(
            settings: new Dictionary<string, string?>
            {
                ["Identity:SignIn:RequireConfirmedEmail"] = "true"
            });

        Assert.True(application.Services
            .GetRequiredService<IOptions<IdentityOptions>>()
            .Value.SignIn.RequireConfirmedEmail);

        await RegisterAsync(application);

        var response = await LoginAsync(application);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task RegisterAsync(MembershipApplication application)
    {
        var response = await application.Client.PostAsJsonAsync(
            "user/register",
            new { email = "juan.perez@example.com", password = "Passw0rd!", firstName = "Juan", lastName = "Pérez" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static Task<HttpResponseMessage> LoginAsync(MembershipApplication application) =>
        application.Client.PostAsJsonAsync(
            "user/login",
            new { email = "juan.perez@example.com", password = "Passw0rd!" },
            TestContext.Current.CancellationToken);

    private static async Task<bool> IsEmailConfirmedAsync(MembershipApplication application)
    {
        using var scope = application.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("juan.perez@example.com");

        return user is not null && await userManager.IsEmailConfirmedAsync(user);
    }
}

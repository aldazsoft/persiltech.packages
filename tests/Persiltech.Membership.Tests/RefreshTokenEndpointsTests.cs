namespace Persiltech.Membership.Tests;

/// <summary>
/// Verifica la renovación y el cierre de sesión contra la aplicación real.
/// </summary>
public sealed class RefreshTokenEndpointsTests
{
    [Fact]
    public async Task LoginDevuelveTestigoDeRenovacion()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();
        var tokens = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.NotEqual(tokens.AccessToken, tokens.RefreshToken);
    }

    [Fact]
    public async Task DosIniciosDeSesionEmitenTestigosDistintos()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        var first = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");
        var second = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    }

    [Fact]
    public async Task RenovarDevuelveParNuevoYElAnteriorDejaDeValer()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();
        var tokens = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        var rotated = await RefreshAsync(application, tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);

        var renewed = (await rotated.Content.ReadFromJsonAsync<LoginUserResponse>(TestContext.Current.CancellationToken))!;

        Assert.False(string.IsNullOrWhiteSpace(renewed.AccessToken));
        Assert.NotEqual(tokens.RefreshToken, renewed.RefreshToken);

        // El testigo nuevo sirve; el consumido ya no. Esa es la rotación.
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(application, renewed.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task ReutilizarUnTestigoConsumidoTumbaLaFamiliaEntera()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();
        var tokens = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        var first = (await (await RefreshAsync(application, tokens.RefreshToken))
            .Content.ReadFromJsonAsync<LoginUserResponse>(TestContext.Current.CancellationToken))!;

        // El testigo original ya se consumió: presentarlo otra vez es indistinguible de un
        // robo, así que cae la familia.
        var replayed = await RefreshAsync(application, tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, replayed.StatusCode);

        var afterBreach = await RefreshAsync(application, first.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, afterBreach.StatusCode);
    }

    [Fact]
    public async Task TestigoDesconocidoDevuelve401()
    {
        await using var application = await MembershipApplication.StartAsync();

        var response = await RefreshAsync(application, "un-testigo-que-nadie-emitio");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TestigoAusenteDevuelve400()
    {
        await using var application = await MembershipApplication.StartAsync();

        var response = await application.Client.PostAsJsonAsync("user/refresh", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CerrarSesionRevocaElTestigo()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();
        var tokens = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        var loggedOut = await application.Client.PostAsJsonAsync(
            "user/logout",
            new { refreshToken = tokens.RefreshToken },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await RefreshAsync(application, tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task CerrarSesionConTestigoDesconocidoTambienDevuelve204()
    {
        await using var application = await MembershipApplication.StartAsync();

        var response = await application.Client.PostAsJsonAsync(
            "user/logout",
            new { refreshToken = "un-testigo-que-nadie-emitio" },
            TestContext.Current.CancellationToken);

        // Un 404 diría a quien pregunta si acertó, que es justo lo que se evita.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CerrarSesionNoAfectaAOtroInicioDeSesion()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        var first = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");
        var second = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        await application.Client.PostAsJsonAsync(
            "user/logout",
            new { refreshToken = first.RefreshToken },
            TestContext.Current.CancellationToken);

        // Cerrar sesión en un dispositivo no debe echar al usuario de los demás.
        Assert.Equal(
            HttpStatusCode.OK,
            (await RefreshAsync(application, second.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task CambiarLaContrasenaRevocaTodasLasSesiones()
    {
        await using var application = await MembershipApplication.StartAsync();

        var accessToken = await application.RegisterAndLoginAsync();
        var tokens = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        var changed = await application.AuthenticatedClient(accessToken).PostAsJsonAsync(
            "password/change",
            new { currentPassword = "Passw0rd!", newPassword = "OtraClave1!" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await RefreshAsync(application, tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task DesactivarLaCuentaImpideRenovar()
    {
        await using var application = await MembershipApplication.StartAsync();

        var administratorToken = await application.RegisterAndLoginAsync(
            "admin@example.com",
            "Passw0rd!");

        await application.RegisterAndLoginAsync();
        var tokens = await application.LoginWithTokensAsync("juan.perez@example.com", "Passw0rd!");

        var client = application.AuthenticatedClient(administratorToken);
        var users = await client.GetFromJsonAsync<JsonElement>("users/paged?page=1&pageSize=50", TestContext.Current.CancellationToken);

        var userId = users.GetProperty("items")
            .EnumerateArray()
            .First(u => u.GetProperty("email").GetString() == "juan.perez@example.com")
            .GetProperty("id")
            .GetString();

        var disabled = await client.PutAsJsonAsync($"users/{userId}/status", new { isActive = false }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await RefreshAsync(application, tokens.RefreshToken)).StatusCode);
    }

    private static async Task<HttpResponseMessage> RefreshAsync(
        MembershipApplication application,
        string refreshToken) =>
        await application.Client.PostAsJsonAsync("user/refresh", new { refreshToken }, TestContext.Current.CancellationToken);
}

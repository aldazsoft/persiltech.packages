namespace Persiltech.Membership.Tests;

/// <summary>
/// La cabecera <c>clientId</c> tiene que llegar desde la petición hasta el aviso.
/// </summary>
/// <remarks>
/// Es el tramo que las pruebas de unidad no cubren: que el endpoint la lea y la ponga en el
/// mensaje. Sin esto, quien redacte el correo nunca sabrá desde qué portal se pidió, y el
/// enlace saldrá siempre hacia el mismo sitio.
/// </remarks>
public class ClientKeyIntegrationTests
{
    [Fact]
    public async Task ThePasswordResetCarriesThePortalThatAskedForIt()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "password/forgot")
        {
            Content = JsonContent.Create(new { email = "juan.perez@example.com" })
        };
        request.Headers.Add(MembershipHeaders.ClientId, "MegadBlazorCustomer");

        var response = await application.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("MegadBlazorCustomer", Assert.Single(application.Messages.Resets).ClientKey);
    }

    [Fact]
    public async Task ThePasswordResetCarriesNoPortalWhenTheHeaderIsMissing()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        var response = await application.Client.PostAsJsonAsync(
            "password/forgot",
            new { email = "juan.perez@example.com" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(Assert.Single(application.Messages.Resets).ClientKey);
    }

    [Fact]
    public async Task TheEmailConfirmationCarriesThePortalThatAskedForIt()
    {
        await using var application = await MembershipApplication.StartAsync();

        await application.RegisterAndLoginAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "email/confirmation/send")
        {
            Content = JsonContent.Create(new { email = "juan.perez@example.com" })
        };
        request.Headers.Add(MembershipHeaders.ClientId, "MegadBlazorCustomer");

        var response = await application.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("MegadBlazorCustomer", application.Messages.Confirmations[^1].ClientKey);
    }
}

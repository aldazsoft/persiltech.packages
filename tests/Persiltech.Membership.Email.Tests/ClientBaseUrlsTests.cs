namespace Persiltech.Membership.Email.Tests;

/// <summary>
/// Elección de la dirección de vuelta cuando el producto tiene más de un portal.
/// </summary>
/// <remarks>
/// El caso que motiva todo esto: con una sola dirección, quien pide su contraseña desde el
/// portal de clientes recibe un enlace que lo lleva al administrativo.
/// </remarks>
public class ClientBaseUrlsTests
{
    private const string Token = "token+/=";
    private const string Default = "https://admin.example.com";
    private const string Customer = "https://clientes.example.com";

    private readonly IEmailSender EmailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer TemplateRenderer = Substitute.For<IEmailTemplateRenderer>();

    private IReadOnlyDictionary<string, string?>? RenderedValues;

    public ClientBaseUrlsTests() =>
        TemplateRenderer.Render(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyDictionary<string, string?>>(values => RenderedValues = values))
            .Returns(new RenderedEmail("Asunto", "<p>Hola</p>", "Hola"));

    [Fact]
    public async Task SendsThePasswordResetToThePortalThatAskedForIt()
    {
        await CreateSender().SendPasswordResetAsync(
            Reset(clientKey: "MegadBlazorCustomer"),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Customer}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendsThePasswordResetToTheAdminPortalWhenItAskedForIt()
    {
        await CreateSender().SendPasswordResetAsync(
            Reset(clientKey: "MegadBlazorAdmin"),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Default}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MatchesTheClientKeyIgnoringCase()
    {
        await CreateSender().SendPasswordResetAsync(
            Reset(clientKey: "megadblazorcustomer"),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Customer}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    /// <summary>
    /// La clave llega en una cabecera que cualquiera puede escribir, así que una desconocida
    /// no puede desviar el enlace: cae en la de por defecto.
    /// </summary>
    [Theory]
    [InlineData("PortalInventado")]
    [InlineData("https://sitio-del-atacante.example")]
    public async Task FallsBackToTheDefaultWhenTheClientIsUnknown(string clientKey)
    {
        await CreateSender().SendPasswordResetAsync(
            Reset(clientKey),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Default}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FallsBackToTheDefaultWhenNoClientCame(string? clientKey)
    {
        await CreateSender().SendPasswordResetAsync(
            Reset(clientKey),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Default}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FallsBackToTheDefaultWhenTheConfiguredUrlIsBlank()
    {
        var sender = CreateSender(options => options.ClientBaseUrls["Vacio"] = "   ");

        await sender.SendPasswordResetAsync(Reset("Vacio"), TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Default}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task KeepsUsingTheDefaultWhenNoPortalWasConfigured()
    {
        var sender = CreateSender(options => options.ClientBaseUrls.Clear());

        await sender.SendPasswordResetAsync(
            Reset("MegadBlazorCustomer"),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Default}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendsTheEmailConfirmationToThePortalThatAskedForIt()
    {
        await CreateSender().SendEmailConfirmationAsync(
            new EmailConfirmationMessage("42", "juan@example.com", "Juan", "Pérez", Token)
            {
                ClientKey = "MegadBlazorCustomer"
            },
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Customer}/confirm-email?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendsTheEmailChangeToThePortalThatAskedForIt()
    {
        await CreateSender().SendEmailChangeAsync(
            new EmailChangeMessage("42", "nuevo@example.com", "Juan", "Pérez", Token)
            {
                ClientKey = "MegadBlazorCustomer"
            },
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Customer}/confirm-email-change?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotDuplicateTheSeparatingSlashOnThePerPortalUrl()
    {
        var sender = CreateSender(options => options.ClientBaseUrls["MegadBlazorCustomer"] = $"{Customer}/");

        await sender.SendPasswordResetAsync(
            Reset("MegadBlazorCustomer"),
            TestContext.Current.CancellationToken);

        Assert.StartsWith($"{Customer}/reset-password?", ActionUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StillCarriesTheEmailAndTheToken()
    {
        await CreateSender().SendPasswordResetAsync(
            Reset("MegadBlazorCustomer"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            $"{Customer}/reset-password?email=juan%40example.com&token=token%2B%2F%3D",
            ActionUrl);
    }

    private string ActionUrl => RenderedValues!["ActionUrl"]!;

    private static PasswordResetMessage Reset(string? clientKey) =>
        new("42", "juan@example.com", "Juan", "Pérez", Token) { ClientKey = clientKey };

    private TemplatedMembershipEmailSender CreateSender(
        Action<MembershipEmailOptions>? configureOptions = null)
    {
        var options = new MembershipEmailOptions
        {
            BrandName = "Persiltech",
            ClientBaseUrl = Default,
            ClientBaseUrls =
            {
                ["MegadBlazorCustomer"] = Customer
            }
        };

        configureOptions?.Invoke(options);

        return new TemplatedMembershipEmailSender(EmailSender, TemplateRenderer, Options.Create(options));
    }
}

namespace Persiltech.Membership.Email.Tests;

public class TemplatedMembershipEmailSenderTests
{
    private const string Token = "token+/=";

    private readonly IEmailSender EmailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer TemplateRenderer = Substitute.For<IEmailTemplateRenderer>();

    private string? RenderedTemplateName;
    private IReadOnlyDictionary<string, string?>? RenderedValues;

    public TemplatedMembershipEmailSenderTests() =>
        TemplateRenderer.Render(
                Arg.Do<string>(templateName => RenderedTemplateName = templateName),
                Arg.Do<IReadOnlyDictionary<string, string?>>(values => RenderedValues = values))
            .Returns(new RenderedEmail("Asunto", "<p>Hola</p>", "Hola"));

    [Fact]
    public async Task SendEmailConfirmationAsync_SendsWhatTheTemplateComposed()
    {
        var sender = CreateSender();

        await sender.SendEmailConfirmationAsync(CreateConfirmationMessage(), CancellationToken.None);

        Assert.Equal("EmailConfirmation", RenderedTemplateName);

        await EmailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(message =>
                message.To == "juan.perez@example.com" &&
                message.Subject == "Asunto" &&
                message.HtmlBody == "<p>Hola</p>" &&
                message.TextBody == "Hola"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_BuildsTheLinkWithTheEmailAndTheToken()
    {
        var sender = CreateSender();

        await sender.SendEmailConfirmationAsync(CreateConfirmationMessage(), CancellationToken.None);

        Assert.Equal(
            "https://app.example.com/confirm-email?email=juan.perez%40example.com&token=token%2B%2F%3D",
            RenderedValues!["ActionUrl"]);
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_PassesTheNameOfTheAccount()
    {
        var sender = CreateSender();

        await sender.SendEmailConfirmationAsync(CreateConfirmationMessage(), CancellationToken.None);

        Assert.Equal("Juan", RenderedValues!["FirstName"]);
        Assert.Equal("Pérez", RenderedValues["LastName"]);
        Assert.Equal("Juan Pérez", RenderedValues["FullName"]);
        Assert.Equal("juan.perez@example.com", RenderedValues["Email"]);
    }

    [Fact]
    public async Task SendPasswordResetAsync_UsesThePasswordResetPath()
    {
        var sender = CreateSender();

        await sender.SendPasswordResetAsync(
            new PasswordResetMessage("42", "juan.perez@example.com", "Juan", "Pérez", Token),
            CancellationToken.None);

        Assert.Equal("PasswordReset", RenderedTemplateName);
        Assert.StartsWith(
            "https://app.example.com/reset-password?email=juan.perez%40example.com",
            RenderedValues!["ActionUrl"],
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendEmailChangeAsync_SendsToTheNewEmail()
    {
        var sender = CreateSender();

        await sender.SendEmailChangeAsync(
            new EmailChangeMessage("42", "nuevo@example.com", "Juan", "Pérez", Token),
            CancellationToken.None);

        Assert.Equal("EmailChange", RenderedTemplateName);
        Assert.StartsWith(
            "https://app.example.com/confirm-email-change?newEmail=nuevo%40example.com",
            RenderedValues!["ActionUrl"],
            StringComparison.Ordinal);

        await EmailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(message => message.To == "nuevo@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_DoesNotDuplicateTheSeparatingSlash()
    {
        var sender = CreateSender(options =>
        {
            options.ClientBaseUrl = "https://app.example.com/";
            options.EmailConfirmationPath = "confirm-email";
        });

        await sender.SendEmailConfirmationAsync(CreateConfirmationMessage(), CancellationToken.None);

        Assert.StartsWith(
            "https://app.example.com/confirm-email?",
            RenderedValues!["ActionUrl"],
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_ThrowsWhenThereIsNoMessage()
    {
        var sender = CreateSender();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.SendEmailConfirmationAsync(null!, CancellationToken.None));
    }

    private static EmailConfirmationMessage CreateConfirmationMessage() =>
        new("42", "juan.perez@example.com", "Juan", "Pérez", Token);

    private TemplatedMembershipEmailSender CreateSender(Action<MembershipEmailOptions>? configureOptions = null)
    {
        var options = new MembershipEmailOptions
        {
            BrandName = "Persiltech",
            ClientBaseUrl = "https://app.example.com"
        };

        configureOptions?.Invoke(options);

        return new TemplatedMembershipEmailSender(EmailSender, TemplateRenderer, Options.Create(options));
    }
}

namespace Persiltech.Email.Tests;

public class SmtpEmailSenderTests
{
    private static readonly EmailMessage Message = new()
    {
        To = "juan.perez@example.com",
        Subject = "Confirma tu correo",
        HtmlBody = "<p>Confirma tu correo.</p>"
    };

    private readonly ISmtpClient Client = Substitute.For<ISmtpClient>();
    private readonly ISmtpClientFactory ClientFactory = Substitute.For<ISmtpClientFactory>();

    private MimeMessage? SentMessage;

    public SmtpEmailSenderTests()
    {
        ClientFactory.Create().Returns(Client);

        Client.SendAsync(
                Arg.Do<MimeMessage>(message => SentMessage = message),
                Arg.Any<CancellationToken>(),
                Arg.Any<ITransferProgress>())
            .Returns(string.Empty);
    }

    [Fact]
    public async Task SendAsync_ConnectsWithTheConfiguredServer()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message, CancellationToken.None);

        await Client.Received(1).ConnectAsync(
            "smtp.example.com",
            587,
            SecureSocketOptions.Auto,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(SmtpSecurity.Auto, SecureSocketOptions.Auto)]
    [InlineData(SmtpSecurity.None, SecureSocketOptions.None)]
    [InlineData(SmtpSecurity.StartTls, SecureSocketOptions.StartTls)]
    [InlineData(SmtpSecurity.SslOnConnect, SecureSocketOptions.SslOnConnect)]
    public async Task SendAsync_TranslatesTheConfiguredSecurity(SmtpSecurity security, SecureSocketOptions expected)
    {
        var sender = CreateSender(options => options.Security = security);

        await sender.SendAsync(Message, CancellationToken.None);

        await Client.Received(1).ConnectAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            expected,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_AuthenticatesWhenThereAreCredentials()
    {
        var sender = CreateSender(options =>
        {
            options.UserName = "no-reply@example.com";
            options.Password = "Passw0rd!";
        });

        await sender.SendAsync(Message, CancellationToken.None);

        await Client.Received(1).AuthenticateAsync(
            "no-reply@example.com",
            "Passw0rd!",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_DoesNotAuthenticateWithoutUserName()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message, CancellationToken.None);

        await Client.DidNotReceive().AuthenticateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ComposesTheMessageWithTheConfiguredSender()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message, CancellationToken.None);

        Assert.NotNull(SentMessage);

        var from = Assert.IsType<MailboxAddress>(Assert.Single(SentMessage.From));
        var to = Assert.IsType<MailboxAddress>(Assert.Single(SentMessage.To));

        Assert.Equal("no-reply@example.com", from.Address);
        Assert.Equal("Persiltech", from.Name);
        Assert.Equal("juan.perez@example.com", to.Address);
        Assert.Equal("Confirma tu correo", SentMessage.Subject);
        Assert.Equal("<p>Confirma tu correo.</p>", SentMessage.HtmlBody);
    }

    [Fact]
    public async Task SendAsync_AddsThePlainTextBodyWhenItComesInTheMessage()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message with { TextBody = "Confirma tu correo." }, CancellationToken.None);

        Assert.NotNull(SentMessage);
        Assert.Equal("Confirma tu correo.", SentMessage.TextBody);
    }

    [Fact]
    public async Task SendAsync_LeavesTheMessageWithoutPlainTextBodyWhenItIsNotProvided()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message, CancellationToken.None);

        Assert.NotNull(SentMessage);
        Assert.Null(SentMessage.TextBody);
    }

    [Fact]
    public async Task SendAsync_AppliesTheConfiguredTimeout()
    {
        var sender = CreateSender(options => options.TimeoutInSeconds = 45);

        await sender.SendAsync(Message, CancellationToken.None);

        Assert.Equal(45_000, Client.Timeout);
    }

    [Fact]
    public async Task SendAsync_ClosesTheConnectionAfterSending()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message, CancellationToken.None);

        await Client.Received(1).DisconnectAsync(true, Arg.Any<CancellationToken>());
        Client.Received(1).Dispose();
    }

    [Fact]
    public async Task SendAsync_AcceptsARecipientWithDisplayName()
    {
        var sender = CreateSender();

        await sender.SendAsync(Message with { To = "Juan Pérez <juan.perez@example.com>" }, CancellationToken.None);

        Assert.NotNull(SentMessage);

        var to = Assert.IsType<MailboxAddress>(Assert.Single(SentMessage.To));

        Assert.Equal("Juan Pérez", to.Name);
        Assert.Equal("juan.perez@example.com", to.Address);
    }

    [Theory]
    [InlineData("juan")]
    [InlineData("juan.perez@")]
    [InlineData("   ")]
    public async Task SendAsync_ThrowsWhenTheRecipientIsNotAValidAddress(string to)
    {
        var sender = CreateSender();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => sender.SendAsync(Message with { To = to }, CancellationToken.None));

        Assert.Equal(nameof(EmailMessage.To), exception.ParamName);
    }

    [Fact]
    public async Task SendAsync_DoesNotConnectWhenTheRecipientIsNotValid()
    {
        var sender = CreateSender();

        await Assert.ThrowsAsync<ArgumentException>(
            () => sender.SendAsync(Message with { To = "juan" }, CancellationToken.None));

        await Client.DidNotReceive().ConnectAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<SecureSocketOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenThereIsNoMessage()
    {
        var sender = CreateSender();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.SendAsync(null!, CancellationToken.None));
    }

    private SmtpEmailSender CreateSender(Action<SmtpOptions>? configureOptions = null)
    {
        var options = new SmtpOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            FromAddress = "no-reply@example.com",
            FromDisplayName = "Persiltech"
        };

        configureOptions?.Invoke(options);

        return new SmtpEmailSender(ClientFactory, Options.Create(options));
    }
}

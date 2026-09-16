namespace Persiltech.Membership.Email.Tests;

public sealed class EmbeddedTemplateRendererTests : IDisposable
{
    private readonly string TemplatesDirectory =
        Path.Combine(Path.GetTempPath(), $"persiltech-membership-email-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(TemplatesDirectory))
        {
            Directory.Delete(TemplatesDirectory, recursive: true);
        }
    }

    [Fact]
    public void Render_ComposesTheSubjectWithTheBrand()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("EmailConfirmation", CreateValues());

        Assert.Equal("Confirma tu correo en Persiltech", rendered.Subject);
    }

    [Fact]
    public void Render_WrapsTheNoticeInTheSharedLayout()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("EmailConfirmation", CreateValues());

        Assert.StartsWith("<!DOCTYPE html", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Confirmar mi correo", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains(DateTime.UtcNow.Year.ToString(), rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_UsesTheSubjectAsPreheader()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("PasswordReset", CreateValues());

        Assert.Contains($"opacity:0;\">{rendered.Subject}<", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EncodesTheValuesInTheHtmlBody()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("EmailConfirmation", CreateValues(firstName: "Juan <b>"));

        Assert.Contains("Juan &lt;b&gt;", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Juan <b>", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_EncodesTheAmpersandsOfTheLink()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("EmailConfirmation", CreateValues());

        Assert.Contains("&amp;token=", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_LeavesThePlainTextBodyWithoutEncoding()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("EmailConfirmation", CreateValues(firstName: "Juan <b>"));

        Assert.Contains("Juan <b>", rendered.TextBody, StringComparison.Ordinal);
        Assert.Contains("&token=", rendered.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WritesTheBrandAsTextWhenThereIsNoLogo()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("EmailConfirmation", CreateValues());

        Assert.DoesNotContain("<img", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains(">Persiltech</span>", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_UsesTheLogoWhenItIsConfigured()
    {
        var renderer = CreateRenderer(options => options.LogoUrl = "https://cdn.example.com/logo.png");

        var rendered = renderer.Render("EmailConfirmation", CreateValues());

        Assert.Contains("<img src=\"https://cdn.example.com/logo.png\"", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_PrefersTheTemplateFromTheConfiguredDirectory()
    {
        WriteTemplate("EmailConfirmation.subject.txt", "Asunto propio de {{BrandName}}");

        var renderer = CreateRenderer(options => options.TemplatesDirectory = TemplatesDirectory);

        var rendered = renderer.Render("EmailConfirmation", CreateValues());

        Assert.Equal("Asunto propio de Persiltech", rendered.Subject);
        Assert.Contains("Confirmar mi correo", rendered.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ThrowsWhenTheTemplateUsesAnUnknownPlaceholder()
    {
        WriteTemplate("EmailConfirmation.subject.txt", "Hola {{Desconocido}}");

        var renderer = CreateRenderer(options => options.TemplatesDirectory = TemplatesDirectory);

        var exception = Assert.Throws<InvalidOperationException>(
            () => renderer.Render("EmailConfirmation", CreateValues()));

        Assert.Contains("Desconocido", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ThrowsWhenTheTemplateDoesNotExist()
    {
        var renderer = CreateRenderer();

        Assert.Throws<InvalidOperationException>(() => renderer.Render("NoExiste", CreateValues()));
    }

    /// <summary>
    /// Las cuatro plantillas del paquete están embebidas y se componen.
    /// </summary>
    /// <remarks>
    /// Cubre el fallo que solo se vería en producción: añadir un aviso, olvidar uno de sus tres
    /// archivos o escribir mal su nombre. El compositor los busca por nombre dentro del
    /// ensamblado, así que un archivo que no viaje no falla al compilar, falla al enviar.
    /// </remarks>
    [Theory]
    [InlineData("EmailConfirmation")]
    [InlineData("PasswordReset")]
    [InlineData("EmailChange")]
    [InlineData("AccountLocked")]
    public void Render_ComposesEveryTemplateOfThePackage(string templateName)
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render(templateName, CreateValues());

        Assert.False(string.IsNullOrWhiteSpace(rendered.Subject));
        Assert.False(string.IsNullOrWhiteSpace(rendered.TextBody));
        Assert.StartsWith("<!DOCTYPE html", rendered.HtmlBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// El aviso del bloqueo dice cuánto dura y no lleva ningún enlace.
    /// </summary>
    [Fact]
    public void Render_TheLockoutNoticeStatesTheDurationAndCarriesNoLink()
    {
        var renderer = CreateRenderer();

        var values = CreateValues();
        values["ActionUrl"] = null;
        values["LockoutMinutes"] = "15";

        var rendered = renderer.Render("AccountLocked", values);

        Assert.Contains("15", rendered.TextBody, StringComparison.Ordinal);
        Assert.Contains("15", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", rendered.TextBody, StringComparison.Ordinal);
    }

    private static EmbeddedTemplateRenderer CreateRenderer(Action<MembershipEmailOptions>? configureOptions = null)
    {
        var options = new MembershipEmailOptions
        {
            BrandName = "Persiltech",
            ClientBaseUrl = "https://app.example.com"
        };

        configureOptions?.Invoke(options);

        return new EmbeddedTemplateRenderer(Options.Create(options));
    }

    private static Dictionary<string, string?> CreateValues(string firstName = "Juan") =>
        new(StringComparer.Ordinal)
        {
            ["FirstName"] = firstName,
            ["LastName"] = "Pérez",
            ["FullName"] = $"{firstName} Pérez",
            ["Email"] = "juan.perez@example.com",
            ["ActionUrl"] = "https://app.example.com/confirm-email?email=juan%40example.com&token=abc",
            // Solo lo usa AccountLocked. Un valor que ninguna plantilla reclama se ignora, pero
            // un marcador sin valor hace fallar al compositor, así que va en el conjunto común.
            ["LockoutMinutes"] = "15"
        };

    private void WriteTemplate(string fileName, string content)
    {
        Directory.CreateDirectory(TemplatesDirectory);

        File.WriteAllText(Path.Combine(TemplatesDirectory, fileName), content);
    }
}

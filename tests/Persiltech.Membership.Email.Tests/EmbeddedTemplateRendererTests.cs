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

    /// <summary>
    /// El asunto se reutiliza como adelanto, el texto que el cliente enseña junto a él en la
    /// bandeja.
    /// </summary>
    /// <remarks>
    /// Se comprueba que el asunto esté dentro del bloque oculto, no la forma exacta del
    /// markup: el adelanto lleva relleno invisible detrás para que no se cuele el principio
    /// del cuerpo, y fijar la cadena entera haría fallar la prueba a cada retoque de estilo.
    /// </remarks>
    [Fact]
    public void Render_UsesTheSubjectAsPreheader()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("PasswordReset", CreateValues());

        var inicio = rendered.HtmlBody.IndexOf("opacity:0", StringComparison.Ordinal);

        Assert.True(inicio >= 0, "No hay ningún bloque oculto que haga de adelanto.");

        var fin = rendered.HtmlBody.IndexOf("</div>", inicio, StringComparison.Ordinal);
        var adelanto = rendered.HtmlBody[inicio..fin];

        Assert.Contains(rendered.Subject, adelanto, StringComparison.Ordinal);
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

    /// <summary>
    /// Sin correo de soporte, el pie no deja separadores colgando ni enlaces vacíos.
    /// </summary>
    /// <remarks>
    /// <c>SupportEmail</c> es opcional, y antes el pie escribía siempre el separador y un
    /// <c>mailto:</c> sin dirección: markup roto en todo despliegue que no lo configurara.
    /// </remarks>
    [Fact]
    public void Render_OmitsTheSupportLineWhenThereIsNoSupportEmail()
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render("PasswordReset", CreateValues());

        Assert.DoesNotContain("mailto:\"", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("&middot;", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Escríbenos", rendered.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_OffersTheSupportEmailWhenThereIsOne()
    {
        var renderer = CreateRenderer(options => options.SupportEmail = "soporte@example.com");

        var rendered = renderer.Render("PasswordReset", CreateValues());

        Assert.Contains("mailto:soporte@example.com", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("soporte@example.com", rendered.TextBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// El idioma del documento sale de la configuración.
    /// </summary>
    /// <remarks>
    /// Estaba fijado a español dentro de la maqueta. Una aplicación que traduzca las
    /// plantillas seguiría anunciando español, y un lector de pantalla las pronunciaría así.
    /// </remarks>
    [Fact]
    public void Render_DeclaresTheConfiguredLanguage()
    {
        var renderer = CreateRenderer(options => options.Language = "en-US");

        var rendered = renderer.Render("PasswordReset", CreateValues());

        Assert.Contains("<html lang=\"en-US\"", rendered.HtmlBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// El texto sobre el color de marca es configurable.
    /// </summary>
    /// <remarks>
    /// El blanco no vale sobre una marca clara: el rótulo del botón y el del encabezado se
    /// vuelven ilegibles, y el paquete no puede elegirlo sin conocer la marca.
    /// </remarks>
    [Fact]
    public void Render_UsesTheConfiguredColorOverTheBrandColor()
    {
        var renderer = CreateRenderer(options =>
        {
            options.PrimaryColor = "#ffe066";
            options.OnPrimaryColor = "#1f2329";
        });

        var rendered = renderer.Render("PasswordReset", CreateValues());

        // En el encabezado, que lo compone el propio compositor, y en el botón, que vive en
        // la plantilla: los dos tienen que respetarlo.
        Assert.Contains("color:#1f2329;\">Persiltech</span>", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("color:#1f2329;font-family:Arial", rendered.HtmlBody, StringComparison.Ordinal);
    }

    /// <summary>
    /// El botón lleva su versión para Outlook de escritorio.
    /// </summary>
    /// <remarks>
    /// Ese cliente usa el motor de Word, que no entiende <c>border-radius</c> ni el relleno de
    /// un <c>inline-block</c>: sin la variante VML, el botón aparece como texto suelto.
    /// </remarks>
    [Theory]
    [InlineData("EmailConfirmation")]
    [InlineData("PasswordReset")]
    [InlineData("EmailChange")]
    public void Render_GivesTheButtonAnOutlookFallback(string templateName)
    {
        var renderer = CreateRenderer();

        var rendered = renderer.Render(templateName, CreateValues());

        Assert.Contains("v:roundrect", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("<!--[if mso]>", rendered.HtmlBody, StringComparison.Ordinal);
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

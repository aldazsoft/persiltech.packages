namespace Persiltech.Membership.Email.Internal;

/// <summary>
/// Compone los avisos sustituyendo marcadores <c>{{Nombre}}</c> sobre las plantillas
/// embebidas en el ensamblado, o sobre las del directorio de sustitución si lo hay.
/// </summary>
/// <param name="options">Marca, rutas y directorio de plantillas.</param>
internal sealed partial class EmbeddedTemplateRenderer(IOptions<MembershipEmailOptions> options)
    : IEmailTemplateRenderer
{
    private const string LayoutFileName = "Layout.html";

    /// <summary>
    /// Codifica solo lo que es sensible en HTML y deja intacto el resto de Unicode: con el
    /// codificador por defecto, cada acento viajaría como una entidad numérica.
    /// </summary>
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static readonly Assembly TemplateAssembly = typeof(EmbeddedTemplateRenderer).Assembly;

    private static readonly string ResourcePrefix = $"{TemplateAssembly.GetName().Name}.Templates.";

    /// <summary>
    /// Marcadores que se insertan sin codificar: son marcado que genera el propio paquete, y
    /// codificarlos los mostraría literales.
    /// </summary>
    private static readonly HashSet<string> RawHtmlKeys = new(StringComparer.Ordinal)
    {
        "Body",
        "BrandHeader"
    };

    private readonly ConcurrentDictionary<string, string> TemplateCache = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public RenderedEmail Render(string templateName, IReadOnlyDictionary<string, string?> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(values);

        var emailOptions = options.Value;
        var merged = BuildValues(emailOptions, values);

        var subject = Substitute(Load($"{templateName}.subject.txt"), merged, encodeHtml: false).Trim();

        merged["Preheader"] = subject;

        var textBody = Substitute(Load($"{templateName}.txt"), merged, encodeHtml: false);

        merged["Body"] = Substitute(Load($"{templateName}.html"), merged, encodeHtml: true);

        var htmlBody = Substitute(Load(LayoutFileName), merged, encodeHtml: true);

        return new RenderedEmail(subject, htmlBody, textBody);
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex TokenPattern();

    private static Dictionary<string, string?> BuildValues(
        MembershipEmailOptions emailOptions,
        IReadOnlyDictionary<string, string?> values)
    {
        var merged = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["BrandName"] = emailOptions.BrandName,
            ["BrandHeader"] = BuildBrandHeader(emailOptions),
            ["PrimaryColor"] = emailOptions.PrimaryColor,
            ["SupportEmail"] = emailOptions.SupportEmail,
            ["Year"] = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture)
        };

        foreach (var value in values)
        {
            merged[value.Key] = value.Value;
        }

        return merged;
    }

    private static string BuildBrandHeader(MembershipEmailOptions emailOptions)
    {
        var brandName = Encoder.Encode(emailOptions.BrandName);

        return string.IsNullOrWhiteSpace(emailOptions.LogoUrl)
            ? $"""<span style="font:bold 20px Arial,sans-serif;color:#ffffff;">{brandName}</span>"""
            : $"""<img src="{Encoder.Encode(emailOptions.LogoUrl)}" alt="{brandName}" height="32" style="display:block;border:0;">""";
    }

    private static string Substitute(
        string template,
        IReadOnlyDictionary<string, string?> values,
        bool encodeHtml) =>
        TokenPattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value;

            if (!values.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException(
                    $"La plantilla usa el marcador '{key}', que no corresponde a ningún valor.");
            }

            return encodeHtml && !RawHtmlKeys.Contains(key)
                ? Encoder.Encode(value ?? string.Empty)
                : value ?? string.Empty;
        });

    private string Load(string fileName) => TemplateCache.GetOrAdd(fileName, ReadTemplate);

    private string ReadTemplate(string fileName)
    {
        var directory = options.Value.TemplatesDirectory;

        if (!string.IsNullOrWhiteSpace(directory))
        {
            var path = Path.Combine(directory, fileName);

            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        using var stream = TemplateAssembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"No se encontró la plantilla '{fileName}'.");

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}

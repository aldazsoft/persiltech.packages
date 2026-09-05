namespace Persiltech.Localizer;

/// <summary>
/// Reads localized values from the resource files that belong to <typeparamref name="TEntity"/>.
/// </summary>
/// <remarks>
/// The resource files are matched by the name of <typeparamref name="TEntity"/>, following the
/// <c>{Extractor}.{Culture}.resx</c> convention: a class named <c>Messages</c> reads from
/// <c>Messages.en-US.resx</c>, <c>Messages.es-PE.resx</c> and so on. The localizer is created
/// once per closed generic type and cached in a static field, so building it is not repeated
/// on every lookup.
/// </remarks>
/// <typeparam name="TEntity">
/// The class that names the resource files. It is used as a marker: no instance of it is created.
/// </typeparam>
public class LocalizationUtils<TEntity>
{
    private static readonly IStringLocalizer StringLocalizer = CreateStringLocalizer();

    /// <summary>
    /// Reads a value for the current UI culture of the thread.
    /// </summary>
    /// <param name="field">The resource key to read.</param>
    /// <returns>
    /// The localized value, or the key itself when no resource file provides it — which is how
    /// <see cref="IStringLocalizer"/> reports a missing entry.
    /// </returns>
    public static string GetValue(string field) => StringLocalizer[field];

    /// <summary>
    /// Reads a value for a specific culture, regardless of the culture of the thread.
    /// </summary>
    /// <remarks>
    /// The culture is applied through a <see cref="CultureScope"/>, so the thread is left as it
    /// was once the value has been read.
    /// </remarks>
    /// <param name="field">The resource key to read.</param>
    /// <param name="cultureinfo">The culture to read the value for.</param>
    /// <returns>
    /// The localized value, or the key itself when no resource file provides it.
    /// </returns>
    public static string GetValue(string field, CultureInfo cultureinfo)
    {
        using (new CultureScope(cultureinfo))
        {
            return GetValue(field);
        }
    }

    private static IStringLocalizer CreateStringLocalizer()
    {
        var localizationOptions = Options.Create(new LocalizationOptions());
        var factory = new ResourceManagerStringLocalizerFactory(localizationOptions, NullLoggerFactory.Instance);

        return factory.Create(typeof(TEntity));
    }
}

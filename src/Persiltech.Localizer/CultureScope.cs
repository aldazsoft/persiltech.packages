namespace Persiltech.Localizer;

/// <summary>
/// Switches the culture of the current thread for the lifetime of the scope and restores it
/// on dispose.
/// </summary>
/// <remarks>
/// Both <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>
/// are changed, and both are restored to the values they had when the scope was created. Use it
/// with a <c>using</c> statement so the restore happens even if the body throws.
/// </remarks>
public class CultureScope : IDisposable
{
    private readonly CultureInfo OriginalCulture;
    private readonly CultureInfo OriginalUICulture;

    /// <summary>
    /// Applies the given culture to the current thread and remembers the previous one.
    /// </summary>
    /// <param name="culture">The culture to apply for the lifetime of the scope.</param>
    public CultureScope(CultureInfo culture)
    {
        OriginalCulture = CultureInfo.CurrentCulture;
        OriginalUICulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>
    /// Restores the culture the current thread had before the scope was created.
    /// </summary>
    public void Dispose()
    {
        CultureInfo.CurrentCulture = OriginalCulture;
        CultureInfo.CurrentUICulture = OriginalUICulture;
    }
}

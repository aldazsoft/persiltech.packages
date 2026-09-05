namespace Persiltech.Localizer.Console.Sample.Resources;

public class Messages
{
    public static string Hello =>
        LocalizationUtils<Messages>.GetValue(nameof(Hello));

    public static string HelloIn(CultureInfo culture) =>
        LocalizationUtils<Messages>.GetValue(nameof(Hello), culture);
}

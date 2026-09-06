namespace Persiltech.Localizer.Tests;

/// <summary>
/// Comprueba que el ámbito cambia la cultura del hilo y la deja como estaba.
/// </summary>
/// <remarks>
/// Cada prueba fija primero una cultura conocida dentro de su propio ámbito, para no
/// depender de la del sistema ni dejar rastro en el hilo si falla.
/// </remarks>
public class CultureScopeTests
{
    private static readonly CultureInfo Peru = new("es-PE");
    private static readonly CultureInfo UnitedStates = new("en-US");
    private static readonly CultureInfo France = new("fr-FR");

    [Fact]
    public void TheScopeAppliesTheCultureToTheThread()
    {
        using var outer = new CultureScope(UnitedStates);

        using (new CultureScope(Peru))
        {
            Assert.Equal(Peru, CultureInfo.CurrentCulture);
            Assert.Equal(Peru, CultureInfo.CurrentUICulture);
        }
    }

    [Fact]
    public void TheScopeRestoresBothCulturesOnDispose()
    {
        using var outer = new CultureScope(UnitedStates);

        using (new CultureScope(Peru))
        {
        }

        Assert.Equal(UnitedStates, CultureInfo.CurrentCulture);
        Assert.Equal(UnitedStates, CultureInfo.CurrentUICulture);
    }

    // El motivo de que el ámbito sea IDisposable: si el cuerpo lanza, el hilo no puede
    // quedarse con la cultura de otro. Un hilo de un pool que se queda en otra cultura
    // contamina toda petición que lo reutilice.
    [Fact]
    public void TheCultureIsRestoredEvenWhenTheBodyThrows()
    {
        using var outer = new CultureScope(UnitedStates);

        // La lambda se declara como Action: un cuerpo que solo lanza deja ambigua la
        // sobrecarga entre Assert.Throws(Action) y Assert.Throws(Func<Task>).
        Action failingScope = () =>
        {
            using (new CultureScope(Peru))
            {
                throw new InvalidOperationException();
            }
        };

        Assert.Throws<InvalidOperationException>(failingScope);

        Assert.Equal(UnitedStates, CultureInfo.CurrentCulture);
        Assert.Equal(UnitedStates, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void NestedScopesRestoreInReverseOrder()
    {
        using var outer = new CultureScope(UnitedStates);

        using (new CultureScope(Peru))
        {
            using (new CultureScope(France))
            {
                Assert.Equal(France, CultureInfo.CurrentCulture);
            }

            Assert.Equal(Peru, CultureInfo.CurrentCulture);
        }

        Assert.Equal(UnitedStates, CultureInfo.CurrentCulture);
    }

    // CurrentCulture gobierna el formato de números y fechas; CurrentUICulture, la
    // búsqueda de recursos. El ámbito mueve ambas, y ambas se restauran por separado.
    [Fact]
    public void BothCulturesAreRestoredIndependently()
    {
        using var outer = new CultureScope(UnitedStates);

        CultureInfo.CurrentUICulture = France;

        using (new CultureScope(Peru))
        {
        }

        Assert.Equal(UnitedStates, CultureInfo.CurrentCulture);
        Assert.Equal(France, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void DisposingTwiceLeavesTheSameCulture()
    {
        using var outer = new CultureScope(UnitedStates);

        var scope = new CultureScope(Peru);
        scope.Dispose();
        scope.Dispose();

        Assert.Equal(UnitedStates, CultureInfo.CurrentCulture);
        Assert.Equal(UnitedStates, CultureInfo.CurrentUICulture);
    }
}

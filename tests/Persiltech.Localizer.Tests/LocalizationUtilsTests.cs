namespace Persiltech.Localizer.Tests;

/// <summary>
/// Comprueba la lectura de recursos contra archivos .resx reales del proyecto de prueba,
/// nombrados por el tipo marcador <see cref="TestMessages"/>.
/// </summary>
/// <remarks>
/// Los recursos son: <c>TestMessages.resx</c> con <c>Greeting</c> y <c>OnlyNeutral</c>,
/// <c>TestMessages.es-PE.resx</c> y <c>TestMessages.en-US.resx</c>, cada uno con su propio
/// <c>Greeting</c>. Esa asimetría es deliberada: permite comprobar tanto la traducción como
/// el respaldo al archivo neutro.
/// </remarks>
public class LocalizationUtilsTests
{
    private static readonly CultureInfo Peru = new("es-PE");
    private static readonly CultureInfo UnitedStates = new("en-US");
    private static readonly CultureInfo France = new("fr-FR");

    [Fact]
    public void TheValueFollowsTheUICultureOfTheThread()
    {
        using (new CultureScope(Peru))
        {
            Assert.Equal("Hola", LocalizationUtils<TestMessages>.GetValue("Greeting"));
        }

        using (new CultureScope(UnitedStates))
        {
            Assert.Equal("Hello there", LocalizationUtils<TestMessages>.GetValue("Greeting"));
        }
    }

    [Fact]
    public void TheOverloadReadsTheCultureItIsGivenRegardlessOfTheThread()
    {
        using (new CultureScope(UnitedStates))
        {
            Assert.Equal("Hola", LocalizationUtils<TestMessages>.GetValue("Greeting", Peru));
        }
    }

    // Es la razón de ser de la sobrecarga: componer un mensaje en la cultura del
    // destinatario sin arrastrar el hilo, que puede estar sirviendo otra petición.
    [Fact]
    public void TheOverloadLeavesTheThreadCultureUntouched()
    {
        using (new CultureScope(UnitedStates))
        {
            LocalizationUtils<TestMessages>.GetValue("Greeting", Peru);

            Assert.Equal(UnitedStates, CultureInfo.CurrentCulture);
            Assert.Equal(UnitedStates, CultureInfo.CurrentUICulture);
        }
    }

    // fr-FR no tiene archivo propio, así que la búsqueda cae al recurso neutro en lugar de
    // devolver la clave: un idioma sin traducir muestra el texto por defecto, no un
    // identificador crudo delante del usuario.
    [Fact]
    public void ACultureWithoutItsOwnFileFallsBackToTheNeutralResource()
    {
        Assert.Equal("Hello", LocalizationUtils<TestMessages>.GetValue("Greeting", France));
    }

    [Fact]
    public void AKeyOnlyInTheNeutralResourceIsFoundFromAnyCulture()
    {
        Assert.Equal("Neutral only", LocalizationUtils<TestMessages>.GetValue("OnlyNeutral", Peru));
        Assert.Equal("Neutral only", LocalizationUtils<TestMessages>.GetValue("OnlyNeutral", UnitedStates));
    }

    // Es el contrato de IStringLocalizer y lo que documenta el paquete: una clave ausente
    // se devuelve tal cual, sin lanzar. Conviene fijarlo, porque consumirlo como si lanzara
    // llevaría a un manejo de errores que nunca se ejecuta.
    [Fact]
    public void AMissingKeyComesBackAsTheKeyItself()
    {
        Assert.Equal("NoExiste", LocalizationUtils<TestMessages>.GetValue("NoExiste", Peru));
    }

    [Fact]
    public void TheLookupIsStableAcrossRepeatedCalls()
    {
        var first = LocalizationUtils<TestMessages>.GetValue("Greeting", Peru);
        var second = LocalizationUtils<TestMessages>.GetValue("Greeting", Peru);

        Assert.Equal(first, second);
        Assert.Equal("Hola", second);
    }
}

namespace Persiltech.Membership.Blazor.Tests;

/// <summary>
/// Los rótulos de los formularios y las dos formas de sustituirlos.
/// </summary>
/// <remarks>
/// Lo que se comprueba aquí es que el paquete sirva sin configurar nada y que, cuando el
/// consumidor cambie una palabra, esa palabra gane <b>en cualquier orden de registro</b>. El
/// orden es lo que se rompe solo: <c>AddMembershipBlazor</c> pone los suyos con <c>TryAdd</c> y
/// <c>AddMembershipFormTexts</c> con <c>Add</c>, así que uno cede por delante y el otro pisa por
/// detrás. Si alguien homogeneiza los dos a la misma llamada, una de las dos mitades deja de
/// funcionar sin que nada falle al compilar.
/// </remarks>
public class MembershipFormTextsTests
{
    private const string BaseAddress = "https://localhost:7082/";

    [Fact]
    public void EveryLabelHasAText()
    {
        var texts = new MembershipFormTexts();

        Assert.All(
            typeof(MembershipFormTexts).GetProperties(),
            property => Assert.False(string.IsNullOrWhiteSpace((string?)property.GetValue(texts))));
    }

    [Fact]
    public void ResolvesTheHouseLabelsWhenNobodyConfiguresThem()
    {
        var texts = Resolve(services => services.AddMembershipBlazor(Configure));

        Assert.Equal("Correo", texts.Email);
        Assert.Equal("Contraseña", texts.Password);
    }

    [Fact]
    public void TheConsumerWinsWhenItRegistersFirst()
    {
        var texts = Resolve(services => services
            .AddMembershipFormTexts(t => t.Email = "Usuario")
            .AddMembershipBlazor(Configure));

        Assert.Equal("Usuario", texts.Email);
    }

    [Fact]
    public void TheConsumerWinsWhenItRegistersLast()
    {
        var texts = Resolve(services => services
            .AddMembershipBlazor(Configure)
            .AddMembershipFormTexts(t => t.Email = "Usuario"));

        Assert.Equal("Usuario", texts.Email);
    }

    /// <summary>
    /// Cambiar uno no borra los demás.
    /// </summary>
    /// <remarks>
    /// El delegado recibe los valores por defecto ya puestos, no una instancia vacía. Es lo que
    /// evita que el consumidor tenga que repetir los once rótulos para cambiar uno.
    /// </remarks>
    [Fact]
    public void TheUntouchedLabelsKeepTheirDefault()
    {
        var texts = Resolve(services => services
            .AddMembershipBlazor(Configure)
            .AddMembershipFormTexts(t => t.Email = "Usuario"));

        Assert.Equal("Contraseña", texts.Password);
        Assert.Equal("Mostrar la contraseña", texts.ShowPassword);
    }

    [Fact]
    public void RejectsANullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddMembershipFormTexts(null!));
    }

    private static void Configure(MembershipApiOptions options) =>
        options.BaseAddress = BaseAddress;

    private static MembershipFormTexts Resolve(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();

        register(services);

        return services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<MembershipFormTexts>>()
            .Value;
    }
}

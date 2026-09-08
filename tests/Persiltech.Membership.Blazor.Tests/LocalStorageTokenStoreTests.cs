namespace Persiltech.Membership.Blazor.Tests;

/// <summary>
/// Verifica el almacén por defecto, y sobre todo que no cachee.
/// </summary>
public sealed class LocalStorageTokenStoreTests
{
    [Fact]
    public async Task SinTokenDeAccesoNoHaySesion()
    {
        var browser = new FakeLocalStorage();

        Assert.Null(await new LocalStorageTokenStore(browser).GetAsync());
    }

    [Fact]
    public async Task GuardaYDevuelveElPar()
    {
        var browser = new FakeLocalStorage();
        var store = new LocalStorageTokenStore(browser);

        await store.SetAsync(new MembershipTokens("acceso", "renovacion"));

        var stored = await store.GetAsync();

        Assert.Equal("acceso", stored?.AccessToken);
        Assert.Equal("renovacion", stored?.RefreshToken);
    }

    [Fact]
    public async Task CerrarBorraLosDos()
    {
        var browser = new FakeLocalStorage();
        var store = new LocalStorageTokenStore(browser);

        await store.SetAsync(new MembershipTokens("acceso", "renovacion"));
        await store.ClearAsync();

        Assert.Null(await store.GetAsync());
        Assert.Empty(browser.Items);
    }

    [Fact]
    public async Task UnaInstanciaVeLoQueOtraGuardo()
    {
        // Es la prueba que importa. IHttpClientFactory resuelve los DelegatingHandler en su
        // propio ámbito, así que el manejador que firma recibe una instancia distinta de la
        // que usa la interfaz. Si el almacén cacheara, la instancia del manejador aprendería
        // "no hay sesión" en la petición de autenticarse y todo lo posterior saldría sin
        // firmar.
        var browser = new FakeLocalStorage();
        var deLaInterfaz = new LocalStorageTokenStore(browser);
        var delManejador = new LocalStorageTokenStore(browser);

        // El manejador pregunta antes de que exista sesión: aquí es donde se cachearía.
        Assert.Null(await delManejador.GetAsync());

        await deLaInterfaz.SetAsync(new MembershipTokens("acceso", "renovacion"));

        Assert.Equal("acceso", (await delManejador.GetAsync())?.AccessToken);
    }

    [Fact]
    public async Task UnTestigoDeRenovacionHuerfanoNoCuentaComoSesion()
    {
        var browser = new FakeLocalStorage();

        browser.Items["persiltech.membership.refreshToken"] = "renovacion";

        Assert.Null(await new LocalStorageTokenStore(browser).GetAsync());
    }

    /// <summary>
    /// El <c>localStorage</c> del navegador, en memoria.
    /// </summary>
    private sealed class FakeLocalStorage : IJSRuntime
    {
        internal Dictionary<string, string> Items { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult(Invoke<TValue>(identifier, args));

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args) =>
            ValueTask.FromResult(Invoke<TValue>(identifier, args));

        private TValue Invoke<TValue>(string identifier, object?[]? args)
        {
            var key = args?[0]?.ToString() ?? string.Empty;

            switch (identifier)
            {
                case "localStorage.getItem":
                    return (TValue)(object)Items.GetValueOrDefault(key)!;

                case "localStorage.setItem":
                    Items[key] = args?[1]?.ToString() ?? string.Empty;
                    break;

                case "localStorage.removeItem":
                    Items.Remove(key);
                    break;

                default:
                    throw new NotSupportedException($"El almacén no llama a '{identifier}'.");
            }

            return default!;
        }
    }
}

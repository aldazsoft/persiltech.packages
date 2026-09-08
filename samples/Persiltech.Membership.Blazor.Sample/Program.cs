var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// El origen de la API es configuración, no una constante: el mismo frontend sirve para el
// sample local y para cualquier despliegue.
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7082/";

// El paquete aporta la sesión entera: el almacén de testigos, el cliente que firma cada
// petición y el estado de autenticación, con su renovación. Es la línea que se verifica.
builder.Services.AddMembershipBlazor(options => options.BaseAddress = apiBaseAddress);

// Lo que el paquete todavía no cubre en la 0.1.0 lo pone el sample: el resto de endpoints y
// el flujo de OAuth. Ambos hablan con la misma API, así que reutilizan su HttpClient con
// nombre, que ya lleva enganchado el manejador que firma.
builder.Services.AddScoped(provider => provider
    .GetRequiredService<IHttpClientFactory>()
    .CreateClient(DependencyInjection.HttpClientName));

builder.Services.AddScoped<SampleApiClient>();
builder.Services.AddScoped<OAuthTokenStore>();
builder.Services.AddScoped<OAuthClient>();

builder.Services.AddAuthorizationCore();
builder.Services.AddMudServices();

await builder.Build().RunAsync();

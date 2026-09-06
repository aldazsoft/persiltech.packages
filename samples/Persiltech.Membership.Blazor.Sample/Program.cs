var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// El origen de la API es configuración, no una constante: el mismo frontend sirve para el
// sample local y para cualquier despliegue.
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7082/";

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<MembershipApiClient>();
builder.Services.AddScoped<OAuthClient>();

builder.Services.AddScoped<MembershipAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    provider => provider.GetRequiredService<MembershipAuthStateProvider>());

builder.Services.AddAuthorizationCore();
builder.Services.AddMudServices();

await builder.Build().RunAsync();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Una API de mentira, dentro del propio sample. El paquete no habla HTTP: recibe los errores
// ya deserializados, así que lo que hay que ver aquí es qué hace con ellos, no cómo viajaron.
builder.Services.AddScoped<FakeApi>();

builder.Services.AddMudServices();

await builder.Build().RunAsync();

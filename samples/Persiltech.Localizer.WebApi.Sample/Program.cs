var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["es-PE", "en-US"];

    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);

    options.ApplyCurrentCultureToResponseHeaders = true;
});

var app = builder.Build();

app.UseRequestLocalization();

app.MapGet("/localizer/message", () => Results.Ok(Messages.Hello));

app.MapGet("/localizer/message/{culture}", (string culture) =>
    Results.Ok(Messages.HelloIn(new CultureInfo(culture))));

app.Run();

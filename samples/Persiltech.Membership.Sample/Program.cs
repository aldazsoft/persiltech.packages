var builder = WebApplication.CreateBuilder(args)
    .AddMembership()
    .AddMembershipOAuth()
    .AddCustomAuth()
    .AddCustomOpenApi()
    .AddMessageSenders()
    .AddCustomCors();

var app = builder.Build();

app.UseCustomCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseCustomOpenApi();

app.MapMembershipEndpoints();
app.MapPasswordEndpoints();
app.MapEmailEndpoints();
app.MapPhoneNumberEndpoints();
app.MapProfileEndpoints();
app.MapTwoFactorEndpoints();
app.MapMembershipOAuthEndpoints();
app.MapAccountEndpoints();

// Los endpoints de administración no traen política: la encadena el consumidor. Se montan
// sobre un grupo porque es donde RequireAuthorization alcanza a varias rutas de una vez.
var administration = app.MapGroup(string.Empty).RequireAuthorization();

administration.MapRoleEndpoints();
administration.MapUserEndpoints();

await app.SeedAsync();

app.Run();

namespace Persiltech.Membership.Sample.Configurations;

internal static class SeedConfiguration
{
    /// <summary>
    /// Sin el administrador inicial, una instalación nueva no tendría ninguna cuenta con la
    /// que usar los endpoints de administración. Las dos operaciones son idempotentes: el
    /// sembrado no reescribe la contraseña si la cuenta ya existe.
    /// </summary>
    internal static async Task<WebApplication> SeedAsync(this WebApplication app)
    {
        await app.Services.SeedMembershipAdministratorAsync(
            new MembershipAdministrator(
                Email: "admin@example.com",
                Password: "Passw0rd!",
                FirstName: "Ada",
                LastName: "Lovelace"));

        await app.Services.RegisterMembershipOAuthClientsAsync(
        [
            new MembershipOAuthClient(
                ClientId: "persiltech-spa",
                DisplayName: "Aplicación de ejemplo",
                RedirectUris: ["https://localhost:7082/callback"],
                Scopes: ["openid", "email", "profile", "roles"])
        ]);

        return app;
    }
}

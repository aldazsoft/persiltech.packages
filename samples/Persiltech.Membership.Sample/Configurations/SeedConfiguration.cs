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

        // Dos clientes, porque son los dos tipos que el servidor distingue y cada uno
        // habilita flujos distintos.
        await app.Services.RegisterMembershipOAuthClientsAsync(
        [
            // Público: no guarda secreto —una aplicación de navegador no puede— y por eso
            // usa Authorization Code con PKCE. Con offline_access recibe además un testigo
            // de renovación, que es lo que permite mantener la sesión sin volver a pedir
            // credenciales.
            new MembershipOAuthClient(
                ClientId: "persiltech-spa",
                DisplayName: "Aplicación de ejemplo",
                RedirectUris: ["https://localhost:7082/callback"],
                Scopes: ["openid", "email", "profile", "roles", "offline_access"]),

            // Confidencial: guarda un secreto, así que puede pedir un testigo para sí mismo
            // con credenciales de cliente. No tiene URI de vuelta porque no hay usuario ni
            // navegador en ese flujo: es un servicio hablando con otro.
            new MembershipOAuthClient(
                ClientId: "persiltech-service",
                DisplayName: "Servicio de ejemplo",
                RedirectUris: [],
                Scopes: ["email"],
                ClientSecret: "un-secreto-de-cliente-solo-para-el-ejemplo")
        ]);

        return app;
    }
}

namespace Persiltech.Membership.Sample.Configurations;

internal static class OAuthConfiguration
{
    internal static WebApplicationBuilder AddMembershipOAuth(
        this WebApplicationBuilder builder)
    {
        // El servidor de autorización guarda sus entidades en su propio contexto, apuntando a la
        // misma base de datos que el paquete base.
        builder.Services.AddMembershipOAuthServer(
            options => options.UseSqlServer(
                builder.Configuration.GetConnectionString("Membership"),
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.FullName)),
            oauth =>
            {
                oauth.LoginPath = "/account/login";
                oauth.UseDevelopmentCertificates = true;
            });

        return builder;
    }
}

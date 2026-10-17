namespace Persiltech.Membership.PostgreSql.Sample.Configurations;

internal static class OAuthConfiguration
{
    internal static WebApplicationBuilder AddMembershipOAuth(
        this WebApplicationBuilder builder)
    {
        // El servidor de autorización guarda sus entidades en su propio contexto, apuntando a la
        // misma base de datos que el paquete base.
        builder.Services.AddMembershipOAuthServer(
            options => options.UseNpgsql(
                builder.Configuration.GetConnectionString("Membership"),
                npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName)),
            oauth =>
            {
                oauth.LoginPath = "/account/login";
                oauth.UseDevelopmentCertificates = true;
            });

        return builder;
    }
}

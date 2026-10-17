namespace Persiltech.Membership.PostgreSql.Sample.Configurations;

internal static class MembershipConfiguration
{
    internal static WebApplicationBuilder AddMembership(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddMembershipServices(
            jwt => builder.Configuration.GetSection("Jwt").Bind(jwt),
            options => options.UseNpgsql(
                builder.Configuration.GetConnectionString("Membership"),
                npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName)));

        return builder;
    }
}

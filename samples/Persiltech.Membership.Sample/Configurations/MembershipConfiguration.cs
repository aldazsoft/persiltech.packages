namespace Persiltech.Membership.Sample.Configurations;

internal static class MembershipConfiguration
{
    internal static WebApplicationBuilder AddMembership(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddMembershipServices(
            jwt => builder.Configuration.GetSection("Jwt").Bind(jwt),
            options => options.UseSqlServer(
                builder.Configuration.GetConnectionString("Membership"),
                sql => sql.MigrationsAssembly(typeof(Program).Assembly.FullName)));

        return builder;
    }
}

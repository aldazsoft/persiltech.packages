namespace Persiltech.Membership.Sample.Configurations;

internal static class AuthConfiguration
{
    /// <summary>
    /// El paquete emite el token; validarlo es del consumidor. Las cookies son el esquema
    /// interactivo que el endpoint de autorización de OAuth necesita: el paquete no monta
    /// ninguno.
    /// </summary>
    internal static WebApplicationBuilder AddCustomAuth(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["Jwt:ValidIssuer"],
                ValidAudience = builder.Configuration["Jwt:ValidAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecurityKey"]!))
            })
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options => options.LoginPath = "/account/login");

        builder.Services.AddAuthorization();

        return builder;
    }
}

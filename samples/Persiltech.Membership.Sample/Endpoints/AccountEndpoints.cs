namespace Persiltech.Membership.Sample.Endpoints;

internal static class AccountEndpoints
{
    /// <summary>
    /// La pantalla de inicio de sesión del flujo interactivo es del consumidor: el paquete
    /// no trae ninguna, porque el flujo Authorization Code exige una sesión de navegador y
    /// el paquete no impone ni interfaz ni maquetación.
    /// </summary>
    internal static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account/login", () => Results.Content(
            """
            <form method="post">
              <input name="email" type="email" />
              <input name="password" type="password" />
              <button type="submit">Entrar</button>
            </form>
            """,
            "text/html"))
            .AllowAnonymous();

        endpoints.MapPost("/account/login", SignInAsync)
            .AllowAnonymous()
            .DisableAntiforgery();

        // Pone a prueba el token recién emitido: sin esta ruta no habría forma de comprobar
        // que el esquema del consumidor lo acepta y que ClaimTypes.Name llega a
        // User.Identity.Name.
        endpoints.MapGet("user/me", (ClaimsPrincipal user) => user.Identity!.Name)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        [FromForm] string email,
        [FromForm] string password,
        string? returnUrl)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return Results.Unauthorized();
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Email!));

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Results.LocalRedirect(returnUrl ?? "/");
    }
}

namespace Persiltech.Membership.OAuth;

/// <summary>
/// Endpoints de Minimal API del servidor de autorización: el de autorización y el de
/// testigos. Las rutas salen de <see cref="MembershipOAuthOptions"/>, no de parámetros:
/// tienen que coincidir con las que se declararon en OpenIddict.
/// </summary>
public static class OAuthEndpoints
{
    private const string OAuthTag = "OAuth2";

    /// <summary>
    /// Monta el endpoint de autorización y el de testigos en las rutas configuradas.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapMembershipOAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<MembershipOAuthOptions>>()
            .Value;

        endpoints.MapMethods(
            options.AuthorizationEndpointPath,
            [HttpMethods.Get, HttpMethods.Post],
            AuthorizeAsync)
            .WithSummary("Iniciar el flujo de autorización")
            .WithDescription("Emite el código de autorización para la aplicación cliente, con PKCE.")
            .WithTags(OAuthTag)
            .AllowAnonymous();

        endpoints.MapPost(options.TokenEndpointPath, ExchangeAsync)
            .WithSummary("Canjear el código o renovar la sesión")
            .WithDescription("Atiende los tipos de concesión authorization_code, refresh_token y client_credentials.")
            .WithTags(OAuthTag)
            .AllowAnonymous();

        endpoints.MapMethods(
            options.UserInfoEndpointPath,
            [HttpMethods.Get, HttpMethods.Post],
            UserInfoAsync)
            .WithSummary("Obtener la información del usuario")
            .WithDescription("Devuelve las reclamaciones del usuario a partir del token de acceso.")
            .WithTags(OAuthTag);

        endpoints.MapMethods(
            options.EndSessionEndpointPath,
            [HttpMethods.Get, HttpMethods.Post],
            EndSessionAsync)
            .WithSummary("Cerrar la sesión")
            .WithDescription("Cierra la sesión interactiva y devuelve al cliente a su URI de salida.")
            .WithTags(OAuthTag)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        Microsoft.Extensions.Options.IOptions<MembershipOAuthOptions> options)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("La petición no es una petición OpenIddict válida.");

        var scheme = options.Value.InteractiveAuthenticationScheme;
        var session = await context.AuthenticateAsync(scheme);

        if (session.Succeeded is not true)
        {
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = context.Request.GetEncodedPathAndQuery() },
                [scheme]);
        }

        var user = await userManager.GetUserAsync(session.Principal!);

        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "La cuenta no está disponible."
                }),
                [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var identity = await CreateIdentityAsync(user, userManager);

        identity.SetScopes(request.GetScopes());

        var principal = new ClaimsPrincipal(identity);
        principal.SetDestinations(GetDestinations);

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("La petición no es una petición OpenIddict válida.");

        if (request.IsClientCredentialsGrantType())
        {
            var machineIdentity = new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name,
                Claims.Role);

            machineIdentity.SetClaim(Claims.Subject, request.ClientId);

            var machinePrincipal = new ClaimsPrincipal(machineIdentity);
            machinePrincipal.SetScopes(request.GetScopes());
            machinePrincipal.SetDestinations(GetDestinations);

            return Results.SignIn(machinePrincipal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return Forbid(Errors.UnsupportedGrantType, "El tipo de concesión no está admitido.");
        }

        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        if (result.Succeeded is not true || result.Principal is null)
        {
            return Forbid(Errors.InvalidGrant, "El código o el testigo de renovación no es válido.");
        }

        var user = await userManager.FindByIdAsync(result.Principal.GetClaim(Claims.Subject) ?? string.Empty);

        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return Forbid(Errors.InvalidGrant, "La cuenta ya no está disponible.");
        }

        var identity = await CreateIdentityAsync(user, userManager);

        identity.SetScopes(result.Principal.GetScopes());

        var principal = new ClaimsPrincipal(identity);
        principal.SetDestinations(GetDestinations);

        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> UserInfoAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var subject = result.Principal?.GetClaim(Claims.Subject);

        if (string.IsNullOrEmpty(subject))
        {
            return Forbid(Errors.InvalidToken, "El token de acceso no es válido.");
        }

        var user = await userManager.FindByIdAsync(subject);

        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return Forbid(Errors.InvalidToken, "La cuenta ya no está disponible.");
        }

        return Results.Ok(new Dictionary<string, object>
        {
            [Claims.Subject] = user.Id,
            [Claims.Email] = user.Email ?? string.Empty,
            [Claims.EmailVerified] = user.EmailConfirmed,
            [Claims.Name] = user.Email ?? string.Empty,
            [Claims.GivenName] = user.FirstName,
            [Claims.FamilyName] = user.LastName,
            [Claims.Role] = await userManager.GetRolesAsync(user)
        });
    }

    private static async Task<IResult> EndSessionAsync(
        HttpContext context,
        Microsoft.Extensions.Options.IOptions<MembershipOAuthOptions> options)
    {
        await context.SignOutAsync(options.Value.InteractiveAuthenticationScheme);

        // OpenIddict valida la URI de salida contra las registradas del cliente y compone
        // la redirección; devolver aquí una propia sería saltarse esa comprobación.
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }

    private static async Task<ClaimsIdentity> CreateIdentityAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager)
    {
        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            Claims.Name,
            Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id)
            .SetClaim(Claims.Email, user.Email)
            .SetClaim(Claims.Name, user.Email)
            .SetClaim(Claims.GivenName, user.FirstName)
            .SetClaim(Claims.FamilyName, user.LastName)
            .SetClaims(Claims.Role, [.. await userManager.GetRolesAsync(user)]);

        return identity;
    }

    private static IResult Forbid(string error, string description) =>
        Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);

    private static IEnumerable<string> GetDestinations(Claim claim) =>
        claim.Type switch
        {
            Claims.Name or Claims.Email or Claims.GivenName or Claims.FamilyName or Claims.Role =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        };
}

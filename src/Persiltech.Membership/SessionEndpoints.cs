namespace Persiltech.Membership;

/// <summary>
/// Endpoints de renovación y cierre de sesión.
/// </summary>
/// <remarks>
/// Los dos son anónimos: el testigo de renovación es la credencial, y exigir además un token
/// de acceso vigente haría imposible renovar justo cuando hace falta, que es cuando el de
/// acceso ya caducó.
/// <para>
/// Cada método tiene dos formas: una genérica en el usuario del consumidor y otra sin
/// parámetros de tipo que la llama con <see cref="ApplicationUser"/>.
/// </para>
/// </remarks>
public static class SessionEndpoints
{
    private const string RefreshPatternByDefault = "user/refresh";
    private const string LogoutPatternByDefault = "user/logout";
    private const string SessionTag = "Session";

    /// <summary>
    /// Monta de una vez los endpoints de renovación y de cierre de sesión.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="refreshPattern">Patrón del endpoint de renovación.</param>
    /// <param name="logoutPattern">Patrón del endpoint de cierre de sesión.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapSessionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string refreshPattern = RefreshPatternByDefault,
        string logoutPattern = LogoutPatternByDefault) =>
        endpoints.MapSessionEndpoints<ApplicationUser>(refreshPattern, logoutPattern);

    /// <summary>
    /// Monta de una vez los endpoints de renovación y de cierre de sesión.
    /// </summary>
    /// <typeparam name="TUser">Usuario de la aplicación.</typeparam>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="refreshPattern">Patrón del endpoint de renovación.</param>
    /// <param name="logoutPattern">Patrón del endpoint de cierre de sesión.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapSessionEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        string refreshPattern = RefreshPatternByDefault,
        string logoutPattern = LogoutPatternByDefault) where TUser : ApplicationUser
    {
        endpoints.MapRefreshTokenEndpoint<TUser>(refreshPattern);
        endpoints.MapLogoutEndpoint(logoutPattern);

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para renovar la sesión.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>
    /// El constructor del endpoint, para que el consumidor lo decore por su cuenta.
    /// </returns>
    public static RouteHandlerBuilder MapRefreshTokenEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapRefreshTokenEndpoint<ApplicationUser>(pattern);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para renovar la sesión.
    /// </summary>
    /// <typeparam name="TUser">Usuario de la aplicación.</typeparam>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>
    /// El constructor del endpoint, para que el consumidor lo decore por su cuenta.
    /// </returns>
    public static RouteHandlerBuilder MapRefreshTokenEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, RefreshAsync<TUser>)
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .WithSummary("Renovar la sesión")
            .WithDescription("Consume el testigo presentado y devuelve un par de tokens nuevo.")
            .WithTags(SessionTag)
            .AllowAnonymous();

    /// <summary>
    /// Monta <c>POST {pattern}</c> para cerrar la sesión.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>
    /// El constructor del endpoint, para que el consumidor lo decore por su cuenta.
    /// </returns>
    /// <remarks>
    /// No lleva parámetro de tipo: revocar una familia de testigos no necesita conocer al
    /// usuario, solo el testigo presentado.
    /// </remarks>
    public static RouteHandlerBuilder MapLogoutEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPost(pattern, LogoutAsync)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Cerrar la sesión")
            .WithDescription("Revoca la familia entera del testigo presentado.")
            .WithTags(SessionTag)
            .AllowAnonymous();

    private static async Task<IResult> RefreshAsync<TUser>(
        RefreshTokenRequest request,
        IRefreshTokenService refreshTokenService,
        UserManager<TUser> userManager,
        IAccessTokenFactory accessTokenFactory,
        CancellationToken cancellationToken) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var rotated = await refreshTokenService.RotateAsync(request.RefreshToken!, cancellationToken);

        if (rotated is null)
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(rotated.UserId);

        // La cuenta pudo desaparecer o bloquearse entre dos renovaciones. Aquí sí cae la
        // familia: el testigo era legítimo, pero la sesión ya no debe continuar.
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await refreshTokenService.RevokeAllForUserAsync(rotated.UserId, cancellationToken);

            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);

        return Results.Ok(
            new LoginUserResponse(
                accessTokenFactory.Create(user, [.. roles]),
                rotated.RefreshToken));
    }

    private static async Task<IResult> LogoutAsync(
        RefreshTokenRequest request,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        await refreshTokenService.RevokeFamilyAsync(request.RefreshToken!, cancellationToken);

        return Results.NoContent();
    }
}

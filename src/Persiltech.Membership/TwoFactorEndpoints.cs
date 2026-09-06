namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API del doble factor por aplicación de autenticación (TOTP). El
/// patrón base es del consumidor: el paquete propone uno por defecto, pero no impone
/// ninguno.
/// </summary>
/// <remarks>
/// Los cuatro operan sobre la cuenta autenticada. La comprobación del segundo factor al
/// entrar no está aquí: viaja en <see cref="LoginUserRequest.TwoFactorCode"/>, en el mismo
/// endpoint de autenticación de siempre.
/// </remarks>
public static class TwoFactorEndpoints
{
    private const string TwoFactorPatternByDefault = "twofactor";
    private const string MembershipTag = "Membership";
    private const int RecoveryCodeCount = 10;

    private static readonly Dictionary<string, string[]> InvalidCode =
        new() { ["code"] = ["El código no es válido."] };

    /// <summary>
    /// Monta los cuatro endpoints de doble factor bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapTwoFactorEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern = TwoFactorPatternByDefault) where TUser : ApplicationUser
    {
        endpoints.MapTwoFactorSetupEndpoint<TUser>($"{pattern}/setup");
        endpoints.MapEnableTwoFactorEndpoint<TUser>($"{pattern}/enable");
        endpoints.MapDisableTwoFactorEndpoint<TUser>($"{pattern}/disable");
        endpoints.MapTwoFactorRecoveryCodesEndpoint<TUser>($"{pattern}/recovery-codes");

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para obtener la clave compartida.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// Genera una clave nueva en cada llamada, de modo que repetirla antes de activar el
    /// doble factor descarta la anterior.
    /// </remarks>
    public static RouteHandlerBuilder MapTwoFactorSetupEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, SetupAsync<TUser>)
            .Produces<TwoFactorSetupResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Preparar el doble factor")
            .WithDescription("Devuelve la clave compartida con la que se da de alta la aplicación de autenticación.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para activar el doble factor.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapEnableTwoFactorEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, EnableAsync<TUser>)
            .Produces<TwoFactorRecoveryCodesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Activar el doble factor")
            .WithDescription("Comprueba el código y activa el doble factor, devolviendo los códigos de recuperación.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para desactivar el doble factor.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapDisableTwoFactorEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, DisableAsync<TUser>)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Desactivar el doble factor")
            .WithDescription("Apaga el doble factor y descarta la clave compartida.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para volver a generar los códigos de recuperación.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapTwoFactorRecoveryCodesEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, RegenerateRecoveryCodesAsync<TUser>)
            .Produces<TwoFactorRecoveryCodesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Regenerar los códigos de recuperación")
            .WithDescription("Descarta los códigos anteriores y devuelve unos nuevos.")
            .WithTags(MembershipTag);

    private static async Task<IResult> SetupAsync<TUser>(
        ClaimsPrincipal principal,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var user = await CurrentUser.FindAsync(principal, userManager);

        if (user is null)
        {
            return Results.NotFound();
        }

        await userManager.ResetAuthenticatorKeyAsync(user);

        var sharedKey = await userManager.GetAuthenticatorKeyAsync(user);

        return Results.Ok(new TwoFactorSetupResponse(sharedKey ?? string.Empty, user.Email ?? string.Empty));
    }

    private static async Task<IResult> EnableAsync<TUser>(
        EnableTwoFactorRequest request,
        ClaimsPrincipal principal,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await CurrentUser.FindAsync(principal, userManager);

        if (user is null)
        {
            return Results.NotFound();
        }

        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            request.Code!);

        if (!valid)
        {
            return Results.ValidationProblem(InvalidCode);
        }

        var enabled = await userManager.SetTwoFactorEnabledAsync(user, true);

        if (!enabled.Succeeded)
        {
            return Results.ValidationProblem(IdentityErrors.ToErrors(enabled));
        }

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);

        return Results.Ok(new TwoFactorRecoveryCodesResponse([.. codes ?? []]));
    }

    private static async Task<IResult> DisableAsync<TUser>(
        ClaimsPrincipal principal,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var user = await CurrentUser.FindAsync(principal, userManager);

        if (user is null)
        {
            return Results.NotFound();
        }

        var disabled = await userManager.SetTwoFactorEnabledAsync(user, false);

        if (!disabled.Succeeded)
        {
            return Results.ValidationProblem(IdentityErrors.ToErrors(disabled));
        }

        // La clave se descarta a propósito: dejarla viva permitiría volver a activar el
        // doble factor con un secreto que el usuario ya dio por retirado.
        await userManager.ResetAuthenticatorKeyAsync(user);

        return Results.NoContent();
    }

    private static async Task<IResult> RegenerateRecoveryCodesAsync<TUser>(
        ClaimsPrincipal principal,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var user = await CurrentUser.FindAsync(principal, userManager);

        if (user is null)
        {
            return Results.NotFound();
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [string.Empty] = ["El doble factor no está activado."]
                });
        }

        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);

        return Results.Ok(new TwoFactorRecoveryCodesResponse([.. codes ?? []]));
    }

    // Las formas sin parámetros de tipo, que son las del caso corriente: llaman a la
    // genérica con ApplicationUser y no hacen nada distinto. Existen para que quien no
    // extienda el usuario componga el paquete sin escribir un solo <>.

    /// <inheritdoc cref="MapTwoFactorEndpoints{TUser}(IEndpointRouteBuilder, string)"/>
    public static IEndpointRouteBuilder MapTwoFactorEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = TwoFactorPatternByDefault) =>
        endpoints.MapTwoFactorEndpoints<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapTwoFactorSetupEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapTwoFactorSetupEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapTwoFactorSetupEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapEnableTwoFactorEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapEnableTwoFactorEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapEnableTwoFactorEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapDisableTwoFactorEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapDisableTwoFactorEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapDisableTwoFactorEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapTwoFactorRecoveryCodesEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapTwoFactorRecoveryCodesEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapTwoFactorRecoveryCodesEndpoint<ApplicationUser>(pattern);
}

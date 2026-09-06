
namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API para la gestión de contraseñas. El patrón base es del
/// consumidor: el paquete propone uno por defecto, pero no impone ninguno.
/// </summary>
public static class PasswordEndpoints
{
    private const string PasswordPatternByDefault = "password";
    private const string MembershipTag = "Membership";

    /// <summary>
    /// Monta los tres endpoints de contraseñas bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapPasswordEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern = PasswordPatternByDefault) where TUser : ApplicationUser
    {
        endpoints.MapChangePasswordEndpoint<TUser>($"{pattern}/change");
        endpoints.MapForgotPasswordEndpoint<TUser>($"{pattern}/forgot");
        endpoints.MapResetPasswordEndpoint<TUser>($"{pattern}/reset");

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para cambiar la contraseña de la cuenta autenticada.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// No llama a <c>AllowAnonymous</c>: opera sobre la cuenta autenticada, que resuelve por
    /// <see cref="ClaimTypes.Name"/>.
    /// </remarks>
    public static RouteHandlerBuilder MapChangePasswordEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, ChangePasswordAsync<TUser>)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Cambiar la contraseña")
            .WithDescription("Cambia la contraseña de la cuenta autenticada.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para pedir el reinicio de una contraseña olvidada.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// Es anónimo: quien ha olvidado su contraseña no tiene token con el que autenticarse.
    /// Responde <c>204</c> exista o no la cuenta, para no convertirse en un verificador de
    /// qué correos están registrados.
    /// </remarks>
    public static RouteHandlerBuilder MapForgotPasswordEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, ForgotPasswordAsync<TUser>)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Pedir el reinicio de la contraseña")
            .WithDescription("Envía el testigo de reinicio si el correo tiene cuenta.")
            .WithTags(MembershipTag)
            .AllowAnonymous();

    /// <summary>
    /// Monta <c>POST {pattern}</c> para fijar la contraseña con el testigo recibido.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapResetPasswordEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, ResetPasswordAsync<TUser>)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Reiniciar la contraseña")
            .WithDescription("Fija una contraseña nueva con el testigo enviado por correo.")
            .WithTags(MembershipTag)
            .AllowAnonymous();

    private static async Task<IResult> ChangePasswordAsync<TUser>(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        UserManager<TUser> userManager,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken) where TUser : ApplicationUser
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

        var result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword!,
            request.NewPassword!);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                IdentityErrors.ToErrors(result, nameof(ChangePasswordRequest.NewPassword)));
        }

        await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> ForgotPasswordAsync<TUser>(
        ForgotPasswordRequest request,
        UserManager<TUser> userManager,
        [FromServices] IMembershipEmailSender emailSender,
        CancellationToken cancellationToken) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await userManager.FindByEmailAsync(request.Email!);

        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            await emailSender.SendPasswordResetAsync(
                new PasswordResetMessage(user.Id, user.Email!, user.FirstName, user.LastName, token),
                cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ResetPasswordAsync<TUser>(
        ResetPasswordRequest request,
        UserManager<TUser> userManager,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await userManager.FindByEmailAsync(request.Email!);

        if (user is null)
        {
            return Results.ValidationProblem(InvalidToken);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token!, request.NewPassword!);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                IdentityErrors.ToErrors(result, nameof(ResetPasswordRequest.Token)));
        }

        await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Results.NoContent();
    }

    private static readonly Dictionary<string, string[]> InvalidToken =
        new() { ["token"] = ["El testigo no es válido."] };

    // Las formas sin parámetros de tipo, que son las del caso corriente: llaman a la
    // genérica con ApplicationUser y no hacen nada distinto. Existen para que quien no
    // extienda el usuario componga el paquete sin escribir un solo <>.

    /// <inheritdoc cref="MapPasswordEndpoints{TUser}(IEndpointRouteBuilder, string)"/>
    public static IEndpointRouteBuilder MapPasswordEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = PasswordPatternByDefault) =>
        endpoints.MapPasswordEndpoints<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapChangePasswordEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapChangePasswordEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapChangePasswordEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapForgotPasswordEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapForgotPasswordEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapForgotPasswordEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapResetPasswordEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapResetPasswordEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapResetPasswordEndpoint<ApplicationUser>(pattern);
}

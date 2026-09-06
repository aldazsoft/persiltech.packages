namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API para el cambio de teléfono. El patrón base es del consumidor:
/// el paquete propone uno por defecto, pero no impone ninguno.
/// </summary>
/// <remarks>
/// Montarlos obliga a registrar <see cref="IMembershipSmsSender"/>; si no se montan, ese
/// puerto no hace falta.
/// </remarks>
public static class PhoneNumberEndpoints
{
    private const string PhonePatternByDefault = "phone";
    private const string MembershipTag = "Membership";

    /// <summary>
    /// Monta los dos endpoints de teléfono bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapPhoneNumberEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern = PhonePatternByDefault) where TUser : ApplicationUser
    {
        endpoints.MapChangePhoneNumberEndpoint<TUser>($"{pattern}/change");
        endpoints.MapConfirmPhoneNumberChangeEndpoint<TUser>($"{pattern}/change/confirm");

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para pedir el cambio de teléfono de la cuenta
    /// autenticada.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapChangePhoneNumberEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, ChangePhoneNumberAsync<TUser>)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Pedir el cambio de teléfono")
            .WithDescription("Envía por SMS al teléfono nuevo el código con el que confirmar el cambio.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para confirmar el cambio de teléfono.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapConfirmPhoneNumberChangeEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, ConfirmPhoneNumberChangeAsync<TUser>)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Confirmar el cambio de teléfono")
            .WithDescription("Aplica el cambio de teléfono con el código enviado por SMS.")
            .WithTags(MembershipTag);

    private static async Task<IResult> ChangePhoneNumberAsync<TUser>(
        ChangePhoneNumberRequest request,
        ClaimsPrincipal principal,
        UserManager<TUser> userManager,
        [FromServices] IMembershipSmsSender smsSender,
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

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber!);

        await smsSender.SendPhoneChangeAsync(
            new PhoneChangeMessage(user.Id, request.PhoneNumber!, user.FirstName, user.LastName, token),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmPhoneNumberChangeAsync<TUser>(
        ConfirmPhoneNumberChangeRequest request,
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

        var result = await userManager.ChangePhoneNumberAsync(user, request.PhoneNumber!, request.Token!);

        return result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(
                IdentityErrors.ToErrors(result, nameof(ConfirmPhoneNumberChangeRequest.Token)));
    }

    // Las formas sin parámetros de tipo, que son las del caso corriente: llaman a la
    // genérica con ApplicationUser y no hacen nada distinto. Existen para que quien no
    // extienda el usuario componga el paquete sin escribir un solo <>.

    /// <inheritdoc cref="MapPhoneNumberEndpoints{TUser}(IEndpointRouteBuilder, string)"/>
    public static IEndpointRouteBuilder MapPhoneNumberEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = PhonePatternByDefault) =>
        endpoints.MapPhoneNumberEndpoints<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapChangePhoneNumberEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapChangePhoneNumberEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapChangePhoneNumberEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapConfirmPhoneNumberChangeEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapConfirmPhoneNumberChangeEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapConfirmPhoneNumberChangeEndpoint<ApplicationUser>(pattern);
}


namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API para la confirmación y el cambio del correo. El patrón base es
/// del consumidor: el paquete propone uno por defecto, pero no impone ninguno.
/// </summary>
public static class EmailEndpoints
{
    private const string EmailPatternByDefault = "email";
    private const string MembershipTag = "Membership";

    private static readonly Dictionary<string, string[]> InvalidToken =
        new() { ["token"] = ["El testigo no es válido."] };

    /// <summary>
    /// Monta los cuatro endpoints de correo bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapEmailEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = EmailPatternByDefault)
    {
        endpoints.MapSendEmailConfirmationEndpoint($"{pattern}/confirmation/send");
        endpoints.MapConfirmEmailEndpoint($"{pattern}/confirmation");
        endpoints.MapChangeEmailEndpoint($"{pattern}/change");
        endpoints.MapConfirmEmailChangeEndpoint($"{pattern}/change/confirm");

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para reenviar la confirmación del correo.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// Es anónimo, porque una cuenta sin confirmar puede no poder entrar todavía. Responde
    /// <c>204</c> exista o no la cuenta, y esté o no ya confirmada.
    /// </remarks>
    public static RouteHandlerBuilder MapSendEmailConfirmationEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPost(pattern, SendEmailConfirmationAsync)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Reenviar la confirmación del correo")
            .WithDescription("Envía el testigo de confirmación si el correo tiene cuenta sin confirmar.")
            .WithTags(MembershipTag)
            .AllowAnonymous();

    /// <summary>
    /// Monta <c>POST {pattern}</c> para confirmar el correo con el testigo recibido.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapConfirmEmailEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPost(pattern, ConfirmEmailAsync)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Confirmar el correo")
            .WithDescription("Confirma el correo de la cuenta con el testigo enviado.")
            .WithTags(MembershipTag)
            .AllowAnonymous();

    /// <summary>
    /// Monta <c>POST {pattern}</c> para pedir el cambio de correo de la cuenta autenticada.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>El aviso va al correo <em>nuevo</em>, que es el que hay que demostrar.</remarks>
    public static RouteHandlerBuilder MapChangeEmailEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPost(pattern, ChangeEmailAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Pedir el cambio de correo")
            .WithDescription("Envía al correo nuevo el testigo con el que confirmar el cambio.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>POST {pattern}</c> para confirmar el cambio de correo.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// Actualiza el correo y el nombre de usuario a la vez: en este paquete el correo
    /// <em>es</em> el nombre de usuario, y dejar el viejo rompería la autenticación.
    /// </remarks>
    public static RouteHandlerBuilder MapConfirmEmailChangeEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPost(pattern, ConfirmEmailChangeAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Confirmar el cambio de correo")
            .WithDescription("Aplica el cambio de correo con el testigo enviado al correo nuevo.")
            .WithTags(MembershipTag);

    private static async Task<IResult> SendEmailConfirmationAsync(
        SendEmailConfirmationRequest request,
        UserManager<ApplicationUser> userManager,
        [FromServices] IMembershipEmailSender emailSender,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await userManager.FindByEmailAsync(request.Email!);

        if (user is not null && !user.EmailConfirmed)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            await emailSender.SendEmailConfirmationAsync(
                new EmailConfirmationMessage(user.Id, user.Email!, user.FirstName, user.LastName, token),
                cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        UserManager<ApplicationUser> userManager)
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

        var result = await userManager.ConfirmEmailAsync(user, request.Token!);

        return result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(IdentityErrors.ToErrors(result, nameof(ConfirmEmailRequest.Token)));
    }

    private static async Task<IResult> ChangeEmailAsync(
        ChangeEmailRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        [FromServices] IMembershipEmailSender emailSender,
        CancellationToken cancellationToken)
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

        var token = await userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail!);

        await emailSender.SendEmailChangeAsync(
            new EmailChangeMessage(user.Id, request.NewEmail!, user.FirstName, user.LastName, token),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> ConfirmEmailChangeAsync(
        ConfirmEmailChangeRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
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

        var changed = await userManager.ChangeEmailAsync(user, request.NewEmail!, request.Token!);

        if (!changed.Succeeded)
        {
            return Results.ValidationProblem(
                IdentityErrors.ToErrors(changed, nameof(ConfirmEmailChangeRequest.Token)));
        }

        var renamed = await userManager.SetUserNameAsync(user, request.NewEmail!);

        return renamed.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(
                IdentityErrors.ToErrors(renamed, nameof(ConfirmEmailChangeRequest.NewEmail)));
    }
}

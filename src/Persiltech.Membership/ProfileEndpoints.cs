namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API del perfil de la cuenta autenticada. El patrón base es del
/// consumidor: el paquete propone uno por defecto, pero no impone ninguno.
/// </summary>
public static class ProfileEndpoints
{
    private const string ProfilePatternByDefault = "profile";
    private const string MembershipTag = "Membership";

    /// <summary>
    /// Monta los dos endpoints de perfil bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    public static IEndpointRouteBuilder MapProfileEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = ProfilePatternByDefault)
    {
        endpoints.MapUpdateProfileEndpoint(pattern);
        endpoints.MapDeleteProfileEndpoint(pattern);

        return endpoints;
    }

    /// <summary>
    /// Monta <c>PUT {pattern}</c> para actualizar el nombre de la cuenta autenticada.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapUpdateProfileEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPut(pattern, UpdateProfileAsync)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Actualizar el perfil")
            .WithDescription("Cambia el nombre y el apellido de la cuenta autenticada.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>DELETE {pattern}</c> para dar de baja la cuenta autenticada.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// La baja <em>borra</em> la cuenta, a diferencia de la operación de administración,
    /// que solo la desactiva. Son dos cosas distintas a propósito: un administrador
    /// suspende a un tercero y quiere poder revertirlo; el titular que se da de baja pide
    /// que sus datos dejen de estar.
    /// <para>
    /// Identity borra en cascada roles, reclamaciones e inicios de sesión externos. Lo que
    /// el paquete <em>no</em> puede tocar son las autorizaciones y testigos que hubiera
    /// emitido un servidor OAuth: viven en otro contexto de datos.
    /// </para>
    /// </remarks>
    public static RouteHandlerBuilder MapDeleteProfileEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapDelete(pattern, DeleteProfileAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Darse de baja")
            .WithDescription("Borra la cuenta autenticada.")
            .WithTags(MembershipTag);

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
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

        user.FirstName = request.FirstName!;
        user.LastName = request.LastName!;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(IdentityErrors.ToErrors(result));
        }

        var roles = await userManager.GetRolesAsync(user);

        return Results.Ok(new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            !await userManager.IsLockedOutAsync(user),
            [.. roles]));
    }

    private static async Task<IResult> DeleteProfileAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await CurrentUser.FindAsync(principal, userManager);

        if (user is null)
        {
            return Results.NotFound();
        }

        var result = await userManager.DeleteAsync(user);

        return result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(IdentityErrors.ToErrors(result));
    }
}

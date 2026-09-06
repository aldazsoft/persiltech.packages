
namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API para consultar y administrar los usuarios. El patrón base es
/// del consumidor: el paquete propone uno por defecto, pero no impone ninguno.
/// </summary>
/// <remarks>
/// Ninguno de estos endpoints llama a <c>AllowAnonymous</c> ni a <c>RequireAuthorization</c>,
/// por la misma razón que los de <see cref="RoleEndpoints"/>: la política de autorización
/// la encadena el consumidor sobre el <see cref="RouteHandlerBuilder"/> que recibe.
/// </remarks>
public static class UserEndpoints
{
    private const string UsersPatternByDefault = "users";
    private const string MembershipTag = "Membership";

    /// <summary>
    /// Monta los cinco endpoints de usuarios bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    /// <remarks>
    /// Con el patrón por defecto monta <c>GET users/current</c>, <c>GET users/{id}</c>,
    /// <c>GET users/paged</c>, <c>PUT users/{id}/status</c> y <c>PUT users/{id}/roles</c>.
    /// </remarks>
    public static IEndpointRouteBuilder MapUserEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern = UsersPatternByDefault) where TUser : ApplicationUser
    {
        endpoints.MapGetCurrentUserEndpoint<TUser>($"{pattern}/current");
        endpoints.MapGetPagedUsersEndpoint<TUser>($"{pattern}/paged");
        endpoints.MapGetUserByIdEndpoint<TUser>($"{pattern}/{{id}}");
        endpoints.MapUpdateUserStatusEndpoint<TUser>($"{pattern}/{{id}}/status");
        endpoints.MapAssignUserRolesEndpoint<TUser>($"{pattern}/{{id}}/roles");

        return endpoints;
    }

    /// <summary>
    /// Monta <c>GET {pattern}</c> para obtener el usuario del token.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// Resuelve la cuenta por <see cref="ClaimTypes.Name"/>, que lleva el correo. No usa el
    /// identificador porque el token que emite el paquete no lo incluye.
    /// </remarks>
    public static RouteHandlerBuilder MapGetCurrentUserEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapGet(pattern, GetCurrentUserAsync<TUser>)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Obtener el usuario actual")
            .WithDescription("Devuelve la cuenta a la que pertenece el token de la petición.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>GET {pattern}</c> para obtener un usuario. El patrón incluye <c>{id}</c>.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta, con el segmento <c>{id}</c>.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapGetUserByIdEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapGet(pattern, GetUserByIdAsync<TUser>)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Obtener un usuario")
            .WithDescription("Devuelve el usuario indicado y sus roles.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>GET {pattern}</c> para obtener una página de usuarios.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapGetPagedUsersEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapGet(pattern, GetPagedUsersAsync<TUser>)
            .Produces<PagedResponse<UserResponse>>(StatusCodes.Status200OK)
            .WithSummary("Listar los usuarios paginados")
            .WithDescription("Devuelve una página de usuarios ordenados por correo, con sus roles.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>PUT {pattern}</c> para activar o desactivar una cuenta. El patrón incluye
    /// <c>{id}</c>.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta, con el segmento <c>{id}</c>.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapUpdateUserStatusEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPut(pattern, UpdateUserStatusAsync<TUser>)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Activar o desactivar una cuenta")
            .WithDescription("Bloquea o desbloquea la cuenta indicada.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>PUT {pattern}</c> para fijar los roles de un usuario. El patrón incluye
    /// <c>{id}</c>.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta, con el segmento <c>{id}</c>.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapAssignUserRolesEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPut(pattern, AssignUserRolesAsync<TUser>)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Fijar los roles de un usuario")
            .WithDescription("Sustituye los roles del usuario por los indicados.")
            .WithTags(MembershipTag);

    private static async Task<IResult> GetCurrentUserAsync<TUser>(
        ClaimsPrincipal principal,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var email = principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            return Results.NotFound();
        }

        var user = await userManager.FindByEmailAsync(email);

        return user is null ? Results.NotFound() : Results.Ok(await ToResponseAsync(user, userManager));
    }

    private static async Task<IResult> GetUserByIdAsync<TUser>(
        string id,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var user = await userManager.FindByIdAsync(id);

        return user is null ? Results.NotFound() : Results.Ok(await ToResponseAsync(user, userManager));
    }

    private static async Task<IResult> GetPagedUsersAsync<TUser>(
        MembershipDbContext<TUser> context,
        int page = 1,
        int pageSize = Paging.PageSizeByDefault) where TUser : ApplicationUser
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var totalCount = await context.Users.CountAsync();

        var users = await context.Users
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var identifiers = users.Select(u => u.Id).ToList();

        var rolesByUser = await context.UserRoles
            .Where(ur => identifiers.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        var items = users
            .Select(user => ToResponse(
                user,
                [.. rolesByUser.Where(r => r.UserId == user.Id).Select(r => r.Name ?? string.Empty)]))
            .ToList();

        return Results.Ok(new PagedResponse<UserResponse>(items, page, pageSize, totalCount));
    }

    private static async Task<IResult> UpdateUserStatusAsync<TUser>(
        string id,
        UpdateUserStatusRequest request,
        UserManager<TUser> userManager,
        IRefreshTokenService refreshTokenService,
        CancellationToken cancellationToken) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return Results.NotFound();
        }

        // Solo se mueve la fecha de fin, nunca el interruptor: apagarlo al activar la
        // cuenta desactivaría también el bloqueo por intentos fallidos, que se apoya en el
        // mismo mecanismo de Identity.
        if (!user.LockoutEnabled)
        {
            var enableLockout = await userManager.SetLockoutEnabledAsync(user, true);

            if (!enableLockout.Succeeded)
            {
                return Results.ValidationProblem(IdentityErrors.ToErrors(enableLockout));
            }
        }

        var endLockout = await userManager.SetLockoutEndDateAsync(
            user,
            request.IsActive!.Value ? null : DateTimeOffset.MaxValue);

        if (!endLockout.Succeeded)
        {
            return Results.ValidationProblem(IdentityErrors.ToErrors(endLockout));
        }

        // Desactivar la cuenta tiene que cerrar lo que ya está abierto: si no, la sesión en
        // curso seguiría renovándose hasta que caducara su testigo.
        if (!request.IsActive.Value)
        {
            await refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
        }

        return Results.Ok(await ToResponseAsync(user, userManager));
    }

    private static async Task<IResult> AssignUserRolesAsync<TUser>(
        string id,
        AssignRolesRequest request,
        UserManager<TUser> userManager,
        RoleManager<IdentityRole> roleManager) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return Results.NotFound();
        }

        var requested = request.Roles!.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        List<string> missing = [];

        foreach (var role in requested)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                missing.Add(role);
            }
        }

        if (missing.Count > 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roles"] = [.. missing.Select(role => $"El rol '{role}' no existe.")]
            });
        }

        var current = await userManager.GetRolesAsync(user);

        var removed = await userManager.RemoveFromRolesAsync(user, current.Except(requested, StringComparer.OrdinalIgnoreCase));

        if (!removed.Succeeded)
        {
            return Results.ValidationProblem(IdentityErrors.ToErrors(removed, nameof(AssignRolesRequest.Roles)));
        }

        var added = await userManager.AddToRolesAsync(user, requested.Except(current, StringComparer.OrdinalIgnoreCase));

        return added.Succeeded
            ? Results.Ok(await ToResponseAsync(user, userManager))
            : Results.ValidationProblem(IdentityErrors.ToErrors(added, nameof(AssignRolesRequest.Roles)));
    }

    private static async Task<UserResponse> ToResponseAsync<TUser>(
        TUser user,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        var roles = await userManager.GetRolesAsync(user);

        return ToResponse(user, [.. roles]);
    }

    private static UserResponse ToResponse(ApplicationUser user, IReadOnlyList<string> roles) =>
        new(user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            !IsLockedOut(user),
            roles);

    private static bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnabled && user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow;


    // Las formas sin parámetros de tipo, que son las del caso corriente: llaman a la
    // genérica con ApplicationUser y no hacen nada distinto. Existen para que quien no
    // extienda el usuario componga el paquete sin escribir un solo <>.

    /// <inheritdoc cref="MapUserEndpoints{TUser}(IEndpointRouteBuilder, string)"/>
    public static IEndpointRouteBuilder MapUserEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = UsersPatternByDefault) =>
        endpoints.MapUserEndpoints<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapGetCurrentUserEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapGetCurrentUserEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapGetCurrentUserEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapGetUserByIdEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapGetUserByIdEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapGetUserByIdEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapGetPagedUsersEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapGetPagedUsersEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapGetPagedUsersEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapUpdateUserStatusEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapUpdateUserStatusEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapUpdateUserStatusEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapAssignUserRolesEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapAssignUserRolesEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapAssignUserRolesEndpoint<ApplicationUser>(pattern);
}

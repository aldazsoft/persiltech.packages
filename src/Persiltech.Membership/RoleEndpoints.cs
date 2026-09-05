namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API para administrar los roles. El patrón base es del consumidor:
/// el paquete propone uno por defecto, pero no impone ninguno.
/// </summary>
/// <remarks>
/// Ninguno de estos endpoints llama a <c>AllowAnonymous</c> ni a <c>RequireAuthorization</c>:
/// son operaciones de administración, pero el paquete no sabe qué políticas tiene el
/// consumidor. Cada método devuelve su <see cref="RouteHandlerBuilder"/> para que sea él
/// quien encadene la suya.
/// </remarks>
public static class RoleEndpoints
{
    private const string RolesPatternByDefault = "roles";
    private const string MembershipTag = "Membership";

    /// <summary>
    /// Monta los seis endpoints de roles bajo el patrón base indicado.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón base del grupo de rutas.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    /// <remarks>
    /// Con el patrón por defecto monta <c>POST roles</c>, <c>PUT roles/{id}</c>,
    /// <c>DELETE roles/{id}</c>, <c>GET roles/{id}</c>, <c>GET roles</c> y
    /// <c>GET roles/paged</c>.
    /// </remarks>
    public static IEndpointRouteBuilder MapRoleEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = RolesPatternByDefault)
    {
        endpoints.MapCreateRoleEndpoint(pattern);
        endpoints.MapGetRolesEndpoint(pattern);
        endpoints.MapGetPagedRolesEndpoint($"{pattern}/paged");
        endpoints.MapGetRoleByIdEndpoint($"{pattern}/{{id}}");
        endpoints.MapUpdateRoleEndpoint($"{pattern}/{{id}}");
        endpoints.MapDeleteRoleEndpoint($"{pattern}/{{id}}");

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para crear un rol.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapCreateRoleEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPost(pattern, CreateRoleAsync)
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Crear un rol")
            .WithDescription("Crea un rol con el nombre indicado.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>PUT {pattern}</c> para renombrar un rol. El patrón incluye <c>{id}</c>.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta, con el segmento <c>{id}</c>.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapUpdateRoleEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapPut(pattern, UpdateRoleAsync)
            .Produces<RoleResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Renombrar un rol")
            .WithDescription("Cambia el nombre del rol indicado.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>DELETE {pattern}</c> para eliminar un rol. El patrón incluye <c>{id}</c>.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta, con el segmento <c>{id}</c>.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapDeleteRoleEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapDelete(pattern, DeleteRoleAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithSummary("Eliminar un rol")
            .WithDescription("Elimina el rol indicado.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>GET {pattern}</c> para obtener un rol. El patrón incluye <c>{id}</c>.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta, con el segmento <c>{id}</c>.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapGetRoleByIdEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapGet(pattern, GetRoleByIdAsync)
            .Produces<RoleResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Obtener un rol")
            .WithDescription("Devuelve el rol indicado.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>GET {pattern}</c> para obtener todos los roles, sin paginar.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    /// <remarks>
    /// Existe para poblar un desplegable en un formulario, que es donde paginar estorba.
    /// Para una pantalla de administración está <see cref="MapGetPagedRolesEndpoint"/>.
    /// </remarks>
    public static RouteHandlerBuilder MapGetRolesEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapGet(pattern, GetRolesAsync)
            .Produces<IReadOnlyList<RoleResponse>>(StatusCodes.Status200OK)
            .WithSummary("Listar los roles")
            .WithDescription("Devuelve todos los roles ordenados por nombre.")
            .WithTags(MembershipTag);

    /// <summary>
    /// Monta <c>GET {pattern}</c> para obtener una página de roles.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>El constructor del endpoint, para que el consumidor lo decore.</returns>
    public static RouteHandlerBuilder MapGetPagedRolesEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapGet(pattern, GetPagedRolesAsync)
            .Produces<PagedResponse<RoleResponse>>(StatusCodes.Status200OK)
            .WithSummary("Listar los roles paginados")
            .WithDescription("Devuelve una página de roles ordenados por nombre.")
            .WithTags(MembershipTag);

    private static async Task<IResult> CreateRoleAsync(
        CreateRoleRequest request,
        RoleManager<IdentityRole> roleManager)
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var role = new IdentityRole(request.Name!);
        var result = await roleManager.CreateAsync(role);

        return result.Succeeded
            ? Results.Created($"/{role.Id}", ToResponse(role))
            : Results.ValidationProblem(IdentityErrors.ToErrors(result, nameof(CreateRoleRequest.Name)));
    }

    private static async Task<IResult> UpdateRoleAsync(
        string id,
        UpdateRoleRequest request,
        RoleManager<IdentityRole> roleManager)
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var role = await roleManager.FindByIdAsync(id);

        if (role is null)
        {
            return Results.NotFound();
        }

        role.Name = request.Name;

        var result = await roleManager.UpdateAsync(role);

        return result.Succeeded
            ? Results.Ok(ToResponse(role))
            : Results.ValidationProblem(IdentityErrors.ToErrors(result, nameof(UpdateRoleRequest.Name)));
    }

    private static async Task<IResult> DeleteRoleAsync(
        string id,
        RoleManager<IdentityRole> roleManager)
    {
        var role = await roleManager.FindByIdAsync(id);

        if (role is null)
        {
            return Results.NotFound();
        }

        var result = await roleManager.DeleteAsync(role);

        return result.Succeeded
            ? Results.NoContent()
            : Results.ValidationProblem(IdentityErrors.ToErrors(result, nameof(UpdateRoleRequest.Name)));
    }

    private static async Task<IResult> GetRoleByIdAsync(
        string id,
        RoleManager<IdentityRole> roleManager)
    {
        var role = await roleManager.FindByIdAsync(id);

        return role is null ? Results.NotFound() : Results.Ok(ToResponse(role));
    }

    private static async Task<IResult> GetRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = await roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse(r.Id, r.Name ?? string.Empty))
            .ToListAsync();

        return Results.Ok(roles);
    }

    private static async Task<IResult> GetPagedRolesAsync(
        RoleManager<IdentityRole> roleManager,
        int page = 1,
        int pageSize = Paging.PageSizeByDefault)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var totalCount = await roleManager.Roles.CountAsync();

        var roles = await roleManager.Roles
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleResponse(r.Id, r.Name ?? string.Empty))
            .ToListAsync();

        return Results.Ok(new PagedResponse<RoleResponse>(roles, page, pageSize, totalCount));
    }

    private static RoleResponse ToResponse(IdentityRole role) =>
        new(role.Id, role.Name ?? string.Empty);
}

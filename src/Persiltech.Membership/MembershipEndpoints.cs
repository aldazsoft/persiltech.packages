namespace Persiltech.Membership;

/// <summary>
/// Endpoints de Minimal API del paquete. El patrón de ruta es del consumidor: el paquete
/// propone unos por defecto, pero no impone ninguno.
/// </summary>
public static class MembershipEndpoints
{
    private const string RegistrationPatternByDefault = "user/register";
    private const string LoginPatternByDefault = "user/login";
    private const string MembershipTag = "Membership";

    private static readonly Dictionary<string, string[]> InvalidCredentials =
        new() { [string.Empty] = ["Credenciales inválidas."] };

    // A diferencia de las credenciales, aquí sí se señala el campo: la contraseña ya se
    // comprobó, así que decir que falta el segundo factor no filtra nada que quien pregunta
    // no supiera ya.
    private static readonly Dictionary<string, string[]> TwoFactorRequired =
        new() { ["twoFactorCode"] = ["Se requiere un código de doble factor válido."] };

    /// <summary>
    /// Monta de una vez los endpoints de registro y de autenticación.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="registrationPattern">Patrón del endpoint de registro.</param>
    /// <param name="loginPattern">Patrón del endpoint de autenticación.</param>
    /// <returns>El mismo constructor de rutas, para poder encadenar.</returns>
    /// <remarks>
    /// Es el atajo para el caso corriente: no hace nada que no se pueda hacer llamando a
    /// <see cref="MapUserRegistrationEndpoint"/> y a <see cref="MapUserLoginEndpoint"/>.
    /// Devuelve el constructor de rutas y no un <see cref="RouteHandlerBuilder"/> porque
    /// monta dos rutas, y ninguna de las dos representaría a la otra.
    /// </remarks>
    public static IEndpointRouteBuilder MapMembershipEndpoints<TUser>(
        this IEndpointRouteBuilder endpoints,
        string registrationPattern = RegistrationPatternByDefault,
        string loginPattern = LoginPatternByDefault) where TUser : ApplicationUser, new()
    {
        endpoints.MapUserRegistrationEndpoint<TUser>(registrationPattern);
        endpoints.MapUserLoginEndpoint<TUser>(loginPattern);

        return endpoints;
    }

    /// <summary>
    /// Monta <c>POST {pattern}</c> para crear una cuenta.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>
    /// El constructor del endpoint, para que el consumidor lo decore por su cuenta.
    /// </returns>
    public static RouteHandlerBuilder MapUserRegistrationEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser, new() =>
        endpoints.MapPost(pattern, RegisterUserAsync<TUser>)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Registrar una cuenta")
            .WithDescription("Crea una cuenta a partir del correo, la contraseña y el nombre.")
            .WithTags(MembershipTag)
            .AllowAnonymous();

    /// <summary>
    /// Monta <c>POST {pattern}</c> para autenticar a un usuario.
    /// </summary>
    /// <param name="endpoints">Constructor de rutas de la aplicación consumidora.</param>
    /// <param name="pattern">Patrón de la ruta.</param>
    /// <returns>
    /// El constructor del endpoint, para que el consumidor lo decore por su cuenta.
    /// </returns>
    public static RouteHandlerBuilder MapUserLoginEndpoint<TUser>(
        this IEndpointRouteBuilder endpoints,
        string pattern) where TUser : ApplicationUser =>
        endpoints.MapPost(pattern, LoginUserAsync<TUser>)
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithSummary("Autenticar a un usuario")
            .WithDescription("Comprueba las credenciales y devuelve un token de acceso.")
            .WithTags(MembershipTag)
            .AllowAnonymous();

    private static async Task<IResult> RegisterUserAsync<TUser>(
        RegisterUserRequest request,
        UserManager<TUser> userManager) where TUser : ApplicationUser, new()
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = new TUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName!,
            LastName = request.LastName!
        };

        var result = await userManager.CreateAsync(user, request.Password!);

        return result.Succeeded
            ? Results.Created()
            : Results.ValidationProblem(ToErrors(result));
    }

    private static async Task<IResult> LoginUserAsync<TUser>(
        LoginUserRequest request,
        UserManager<TUser> userManager,
        IAccessTokenFactory accessTokenFactory,
        IRefreshTokenService refreshTokenService,
        IOptions<IdentityOptions> identityOptions,
        CancellationToken cancellationToken) where TUser : ApplicationUser
    {
        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var user = await userManager.FindByEmailAsync(request.Email!);

        if (user is null)
        {
            return Results.ValidationProblem(InvalidCredentials);
        }

        // El bloqueo se comprueba antes de la contraseña: si no, cada intento sobre una
        // cuenta ya bloqueada seguiría incrementando el contador y la mantendría bloqueada
        // indefinidamente.
        if (await userManager.IsLockedOutAsync(user))
        {
            return Results.ValidationProblem(InvalidCredentials);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password!))
        {
            await userManager.AccessFailedAsync(user);

            return Results.ValidationProblem(InvalidCredentials);
        }

        if (await userManager.GetTwoFactorEnabledAsync(user) &&
            !await VerifySecondFactorAsync(user, request.TwoFactorCode, userManager))
        {
            await userManager.AccessFailedAsync(user);

            return Results.ValidationProblem(TwoFactorRequired);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var signIn = identityOptions.Value.SignIn;

        if ((signIn.RequireConfirmedEmail && !user.EmailConfirmed) ||
            (signIn.RequireConfirmedPhoneNumber && !user.PhoneNumberConfirmed))
        {
            return Results.ValidationProblem(InvalidCredentials);
        }

        var roles = await userManager.GetRolesAsync(user);
        var refreshToken = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return Results.Ok(
            new LoginUserResponse(accessTokenFactory.Create(user, [.. roles]), refreshToken));
    }

    private static async Task<bool> VerifySecondFactorAsync<TUser>(
        TUser user,
        string? code,
        UserManager<TUser> userManager) where TUser : ApplicationUser
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        if (await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                code))
        {
            return true;
        }

        // Un código de recuperación es la salida cuando se pierde el teléfono. Se consume
        // al usarlo, así que no sirve dos veces.
        var redeemed = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);

        return redeemed.Succeeded;
    }

    private static Dictionary<string, string[]> ToErrors(IdentityResult result)
    {
        Dictionary<string, List<string>> messages = [];

        foreach (var error in result.Errors)
        {
            var key = ErrorKey(error.Code);

            if (!messages.TryGetValue(key, out var accumulated))
            {
                accumulated = [];
                messages[key] = accumulated;
            }

            accumulated.Add(error.Description);
        }

        return messages.ToDictionary(m => m.Key, m => m.Value.ToArray());
    }

    private static string ErrorKey(string code) =>
        code.StartsWith("Password", StringComparison.Ordinal) ? "password"
        : code is "DuplicateUserName" or "DuplicateEmail" ? "email"
        : string.Empty;

    // Las formas sin parámetros de tipo, que son las del caso corriente: llaman a la
    // genérica con ApplicationUser y no hacen nada distinto. Existen para que quien no
    // extienda el usuario componga el paquete sin escribir un solo <>.

    /// <inheritdoc cref="MapMembershipEndpoints{TUser}(IEndpointRouteBuilder, string, string)"/>
    public static IEndpointRouteBuilder MapMembershipEndpoints(
        this IEndpointRouteBuilder endpoints,
        string registrationPattern = RegistrationPatternByDefault,
        string loginPattern = LoginPatternByDefault) =>
        endpoints.MapMembershipEndpoints<ApplicationUser>(registrationPattern, loginPattern);

    /// <inheritdoc cref="MapUserRegistrationEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapUserRegistrationEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapUserRegistrationEndpoint<ApplicationUser>(pattern);

    /// <inheritdoc cref="MapUserLoginEndpoint{TUser}(IEndpointRouteBuilder, string)"/>
    public static RouteHandlerBuilder MapUserLoginEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern) =>
        endpoints.MapUserLoginEndpoint<ApplicationUser>(pattern);
}

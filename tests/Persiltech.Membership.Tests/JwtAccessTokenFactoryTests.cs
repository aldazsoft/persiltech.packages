namespace Persiltech.Membership.Tests;

public class JwtAccessTokenFactoryTests
{
    private const string SecurityKey = "una-clave-de-firma-de-32-caracteres";
    private const string ValidIssuer = "https://membership.persiltech.test";
    private const string ValidAudience = "persiltech-sample";

    private static readonly ApplicationUser User = new()
    {
        UserName = "juan.perez@example.com",
        Email = "juan.perez@example.com",
        FirstName = "Juan",
        LastName = "Pérez"
    };

    [Fact]
    public async Task CreateEmitsTheAgreedClaims()
    {
        var token = Read(await CreateFactory().CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal(User.Email, token.GetClaim(ClaimTypes.Name).Value);
        Assert.Equal("Juan Pérez", token.GetClaim("Fullname").Value);
        Assert.Equal(ValidIssuer, token.Issuer);
        Assert.Equal(ValidAudience, Assert.Single(token.Audiences));
    }

    [Fact]
    public async Task CreateEmitsNoOtherClaim()
    {
        var token = Read(await CreateFactory().CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal(
            new HashSet<string> { ClaimTypes.Name, "Fullname", "iss", "aud", "exp", "nbf", "iat" },
            token.Claims.Select(claim => claim.Type).ToHashSet());
    }

    [Fact]
    public async Task CreateExpiresTheTokenAfterTheConfiguredMinutes()
    {
        var token = Read(await CreateFactory(expireInMinutes: 45).CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal(45, Math.Round((token.ValidTo - token.ValidFrom).TotalMinutes));
    }

    [Fact]
    public async Task CreateSignsTheTokenWithTheConfiguredKey()
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            await CreateFactory().CreateAsync(User, [], TestContext.Current.CancellationToken),
            new TokenValidationParameters
            {
                ValidIssuer = ValidIssuer,
                ValidAudience = ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey))
            });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateSignsTheTokenSoThatAnotherKeyRejectsIt()
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            await CreateFactory().CreateAsync(User, [], TestContext.Current.CancellationToken),
            new TokenValidationParameters
            {
                ValidIssuer = ValidIssuer,
                ValidAudience = ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("otra-clave-de-firma-de-32-caracteres"))
            });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateEmitsOneRoleClaimForEachRole()
    {
        var token = Read(await CreateFactory().CreateAsync(User, ["Administrators", "Auditors"], TestContext.Current.CancellationToken));

        Assert.Equal(
            ["Administrators", "Auditors"],
            token.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value));
    }

    [Fact]
    public async Task CreateEmitsNoRoleClaimWhenTheUserHasNoRoles()
    {
        var token = Read(await CreateFactory().CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.DoesNotContain(token.Claims, claim => claim.Type == ClaimTypes.Role);
    }

    [Fact]
    public async Task CreateAddsTheClaimsThatAProviderContributes()
    {
        var factory = CreateFactory(providers: [Provider(("CustomerId", "42"), ("CustomerCode", "dev"))]);

        var token = Read(await factory.CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal("42", token.GetClaim("CustomerId").Value);
        Assert.Equal("dev", token.GetClaim("CustomerCode").Value);
    }

    [Fact]
    public async Task CreateAddsTheClaimsOfEveryProvider()
    {
        var factory = CreateFactory(providers:
        [
            Provider(("CustomerId", "42")),
            Provider(("Tier", "premium"))
        ]);

        var token = Read(await factory.CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal("42", token.GetClaim("CustomerId").Value);
        Assert.Equal("premium", token.GetClaim("Tier").Value);
    }

    [Fact]
    public async Task CreateKeepsEmittingItsOwnClaimsAlongsideTheContributedOnes()
    {
        var factory = CreateFactory(providers: [Provider(("CustomerId", "42"))]);

        var token = Read(await factory.CreateAsync(User, ["Administrators"], TestContext.Current.CancellationToken));

        Assert.Equal(User.Email, token.GetClaim(ClaimTypes.Name).Value);
        Assert.Equal("Juan Pérez", token.GetClaim("Fullname").Value);
        Assert.Equal("Administrators", token.GetClaim(ClaimTypes.Role).Value);
        Assert.Equal("42", token.GetClaim("CustomerId").Value);
    }

    [Fact]
    public async Task CreateIgnoresAProviderThatContributesNothing()
    {
        var factory = CreateFactory(providers: [Provider()]);

        var token = Read(await factory.CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal(
            new HashSet<string> { ClaimTypes.Name, "Fullname", "iss", "aud", "exp", "nbf", "iat" },
            token.Claims.Select(claim => claim.Type).ToHashSet());
    }

    [Fact]
    public async Task CreateIgnoresAProviderThatReturnsNull()
    {
        var factory = CreateFactory(providers: [new NullClaimsProvider()]);

        var token = Read(await factory.CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Equal(User.Email, token.GetClaim(ClaimTypes.Name).Value);
    }

    /// <summary>
    /// El caso que de verdad importa: sin este corte, quien pudiera registrar un proveedor
    /// podría concederse cualquier rol.
    /// </summary>
    [Theory]
    [InlineData("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")]
    [InlineData("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")]
    [InlineData("Fullname")]
    public async Task CreateRefusesToLetAProviderOverwriteAReservedClaim(string reserved)
    {
        var factory = CreateFactory(providers: [Provider((reserved, "usurpado"))]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateAsync(User, ["Users"], TestContext.Current.CancellationToken));

        Assert.Contains(reserved, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRefusesTwoProvidersThatContributeTheSameClaim()
    {
        var factory = CreateFactory(providers:
        [
            Provider(("CustomerId", "42")),
            Provider(("CustomerId", "43"))
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateAsync(User, [], TestContext.Current.CancellationToken));

        Assert.Contains("CustomerId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRefusesAClaimWithoutName()
    {
        var factory = CreateFactory(providers: [Provider((" ", "algo"))]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync(User, [], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSignsTheContributedClaimsToo()
    {
        var factory = CreateFactory(providers: [Provider(("CustomerId", "42"))]);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            await factory.CreateAsync(User, [], TestContext.Current.CancellationToken),
            new TokenValidationParameters
            {
                ValidIssuer = ValidIssuer,
                ValidAudience = ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey))
            });

        Assert.True(result.IsValid);
        Assert.Equal("42", result.ClaimsIdentity.FindFirst("CustomerId")!.Value);
    }

    [Fact]
    public async Task CreatePassesTheCancellationTokenToTheProvider()
    {
        var provider = new RecordingClaimsProvider();
        var factory = CreateFactory(providers: [provider]);

        using var cancellation = new CancellationTokenSource();

        await factory.CreateAsync(User, [], cancellation.Token);

        Assert.Equal(cancellation.Token, provider.ReceivedToken);
    }

    private static JsonWebToken Read(string accessToken) =>
        new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

    private static JwtAccessTokenFactory CreateFactory(
        int expireInMinutes = 30,
        IEnumerable<IAccessTokenClaimsProvider>? providers = null) =>
        new(
            Options.Create(new JwtOptions
            {
                SecurityKey = SecurityKey,
                ValidIssuer = ValidIssuer,
                ValidAudience = ValidAudience,
                ExpireInMinutes = expireInMinutes
            }),
            providers ?? []);

    private static IAccessTokenClaimsProvider Provider(params (string Name, string Value)[] claims) =>
        new StubClaimsProvider(claims.ToDictionary(c => c.Name, c => c.Value, StringComparer.Ordinal));

    private sealed class StubClaimsProvider(IReadOnlyDictionary<string, string> claims)
        : IAccessTokenClaimsProvider
    {
        public Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) => Task.FromResult(claims);
    }

    private sealed class NullClaimsProvider : IAccessTokenClaimsProvider
    {
        public Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(null!);
    }

    private sealed class RecordingClaimsProvider : IAccessTokenClaimsProvider
    {
        public CancellationToken ReceivedToken { get; private set; }

        public Task<IReadOnlyDictionary<string, string>> GetClaimsAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;

            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.Ordinal));
        }
    }
}

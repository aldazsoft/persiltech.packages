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
    public void CreateEmitsTheAgreedClaims()
    {
        var token = Read(CreateFactory().Create(User, []));

        Assert.Equal(User.Email, token.GetClaim(ClaimTypes.Name).Value);
        Assert.Equal("Juan Pérez", token.GetClaim("Fullname").Value);
        Assert.Equal(ValidIssuer, token.Issuer);
        Assert.Equal(ValidAudience, Assert.Single(token.Audiences));
    }

    [Fact]
    public void CreateEmitsNoOtherClaim()
    {
        var token = Read(CreateFactory().Create(User, []));

        Assert.Equal(
            new HashSet<string> { ClaimTypes.Name, "Fullname", "iss", "aud", "exp", "nbf", "iat" },
            token.Claims.Select(claim => claim.Type).ToHashSet());
    }

    [Fact]
    public void CreateExpiresTheTokenAfterTheConfiguredMinutes()
    {
        var token = Read(CreateFactory(expireInMinutes: 45).Create(User, []));

        Assert.Equal(45, Math.Round((token.ValidTo - token.ValidFrom).TotalMinutes));
    }

    [Fact]
    public async Task CreateSignsTheTokenWithTheConfiguredKey()
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            CreateFactory().Create(User, []),
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
            CreateFactory().Create(User, []),
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
    public void CreateEmitsOneRoleClaimForEachRole()
    {
        var token = Read(CreateFactory().Create(User, ["Administrators", "Auditors"]));

        Assert.Equal(
            ["Administrators", "Auditors"],
            token.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value));
    }

    [Fact]
    public void CreateEmitsNoRoleClaimWhenTheUserHasNoRoles()
    {
        var token = Read(CreateFactory().Create(User, []));

        Assert.DoesNotContain(token.Claims, claim => claim.Type == ClaimTypes.Role);
    }

    private static JsonWebToken Read(string accessToken) =>
        new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

    private static JwtAccessTokenFactory CreateFactory(int expireInMinutes = 30) =>
        new(Options.Create(new JwtOptions
        {
            SecurityKey = SecurityKey,
            ValidIssuer = ValidIssuer,
            ValidAudience = ValidAudience,
            ExpireInMinutes = expireInMinutes
        }));
}

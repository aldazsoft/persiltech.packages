namespace Persiltech.Membership.Internal;

/// <summary>
/// Almacena los testigos de renovación en el contexto de datos del consumidor.
/// </summary>
/// <remarks>
/// Va contra el mismo contexto que Identity, y no contra un almacén aparte, para que el
/// paquete no imponga infraestructura que el consumidor no haya elegido ya.
/// </remarks>
/// <typeparam name="TUser">Usuario de la aplicación.</typeparam>
/// <param name="dbContext">Contexto de datos donde vive la tabla.</param>
/// <param name="jwtOptions">Opciones de las que sale la vigencia del testigo.</param>
internal sealed class RefreshTokenService<TUser>(
    MembershipDbContext<TUser> dbContext,
    IOptions<JwtOptions> jwtOptions) : IRefreshTokenService where TUser : ApplicationUser
{
    private const int TokenSizeInBytes = 32;

    /// <inheritdoc />
    public async Task<string> IssueAsync(string userId, CancellationToken cancellationToken) =>
        await CreateAsync(userId, Guid.NewGuid(), cancellationToken);

    /// <inheritdoc />
    public async Task<RotatedRefreshToken?> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);

        if (stored is null || stored.RevokedAt is not null)
        {
            return null;
        }

        // Un testigo ya consumido solo se presenta si el cliente perdió la respuesta de la
        // rotación anterior o si alguien lo robó y lo está usando en paralelo. No se pueden
        // distinguir, así que se asume lo segundo y cae la familia entera.
        if (stored.ConsumedAt is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, cancellationToken);

            return null;
        }

        if (stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        stored.ConsumedAt = DateTimeOffset.UtcNow;

        var issued = await CreateAsync(stored.UserId, stored.FamilyId, cancellationToken);

        return new RotatedRefreshToken(stored.UserId, issued);
    }

    /// <inheritdoc />
    public async Task RevokeFamilyAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await FindAsync(refreshToken, cancellationToken);

        if (stored is null)
        {
            return;
        }

        await RevokeFamilyAsync(stored.FamilyId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken)
    {
        await dbContext.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private async Task<string> CreateAsync(
        string userId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        var token = GenerateToken();
        var now = DateTimeOffset.UtcNow;

        dbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(token),
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenExpireInDays)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        await dbContext.Set<RefreshToken>()
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private async Task<RefreshToken?> FindAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);

        return await dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    private static string GenerateToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

    private static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

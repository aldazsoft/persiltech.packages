namespace Persiltech.Membership.Internal;

/// <summary>
/// Resultado de una rotación correcta.
/// </summary>
/// <param name="UserId">Cuenta a la que pertenecía el testigo consumido.</param>
/// <param name="RefreshToken">El testigo nuevo, en claro.</param>
internal sealed record RotatedRefreshToken(string UserId, string RefreshToken);

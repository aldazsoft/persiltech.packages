namespace Persiltech.Membership.Responses;

/// <summary>
/// Códigos de recuperación de un solo uso.
/// </summary>
/// <param name="RecoveryCodes">
/// Códigos generados. Se entregan <em>una sola vez</em>: el almacén guarda su forma
/// verificable, no el texto, y no hay forma de volver a mostrarlos.
/// </param>
public sealed record TwoFactorRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

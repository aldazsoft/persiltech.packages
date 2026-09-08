namespace Persiltech.Membership.Blazor.Contracts;

/// <summary>
/// Ausencia de valor, para las llamadas que responden <c>204</c>.
/// </summary>
/// <remarks>
/// <see cref="ApiResult{T}"/> siempre tiene un tipo, y usar <see cref="object"/> para las
/// operaciones sin cuerpo dejaría en la firma un valor que nunca llega. Este tipo lo dice.
/// </remarks>
public sealed record Unit
{
    private Unit()
    {
    }
}

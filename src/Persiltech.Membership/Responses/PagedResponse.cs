namespace Persiltech.Membership.Responses;

/// <summary>
/// Página de resultados de una consulta paginada.
/// </summary>
/// <typeparam name="T">Tipo de los elementos de la página.</typeparam>
/// <param name="Items">Elementos de esta página.</param>
/// <param name="Page">Número de página, de base 1.</param>
/// <param name="PageSize">Cantidad de elementos por página.</param>
/// <param name="TotalCount">Cantidad total de elementos, en todas las páginas.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>
    /// Cantidad total de páginas.
    /// </summary>
    /// <remarks>
    /// Se calcula en lugar de recibirse, para que no pueda contradecir a
    /// <see cref="TotalCount"/> ni a <see cref="PageSize"/>.
    /// </remarks>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

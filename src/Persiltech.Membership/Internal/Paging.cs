namespace Persiltech.Membership.Internal;

/// <summary>
/// Normaliza los parámetros de paginación que llegan por la cadena de consulta.
/// </summary>
internal static class Paging
{
    /// <summary>
    /// Cantidad de elementos por página cuando el cliente no indica ninguna.
    /// </summary>
    internal const int PageSizeByDefault = 20;

    /// <summary>
    /// Cantidad máxima de elementos por página.
    /// </summary>
    internal const int PageSizeLimit = 100;

    /// <summary>
    /// Acota los parámetros recibidos a un rango utilizable.
    /// </summary>
    /// <param name="page">Número de página pedido.</param>
    /// <param name="pageSize">Cantidad de elementos por página pedida.</param>
    /// <returns>Los dos valores ya acotados.</returns>
    /// <remarks>
    /// Se acota en lugar de rechazar con un <c>400</c> porque un error por un parámetro de
    /// paginación es más ruidoso que útil. El techo no es opcional: sin él, un
    /// <c>pageSize</c> arbitrario convierte un endpoint de administración en una forma de
    /// tumbar la base de datos.
    /// </remarks>
    internal static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (page < 1 ? 1 : page,
         pageSize < 1 ? PageSizeByDefault : pageSize > PageSizeLimit ? PageSizeLimit : pageSize);
}

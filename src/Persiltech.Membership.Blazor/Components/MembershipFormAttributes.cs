namespace Persiltech.Membership.Blazor.Components;

/// <summary>
/// Atributos sueltos que los cuatro formularios ponen en los mismos sitios.
/// </summary>
/// <remarks>
/// Viven aquí y no repetidos en cada componente porque son detalles que se olvidan al añadir
/// un formulario nuevo, y su ausencia no rompe nada visible: simplemente deja fuera a quien no
/// ve la pantalla.
/// </remarks>
internal static class MembershipFormAttributes
{
    /// <summary>
    /// Lo que convierte un aviso de error en algo que un lector de pantalla anuncia al salir.
    /// </summary>
    /// <remarks>
    /// Sin esto, el aviso aparece en el documento y nadie lo lee: quien no ve la pantalla se
    /// queda esperando sin saber que el intento falló. <c>assertive</c> y no <c>polite</c>
    /// porque interrumpe a propósito: es la respuesta a lo que esa persona acaba de pedir.
    /// </remarks>
    internal static Dictionary<string, object?> Alert => new()
    {
        ["role"] = "alert",
        ["aria-live"] = "assertive"
    };

    /// <summary>
    /// Marca un campo para que el navegador y el gestor de contraseñas sepan qué es.
    /// </summary>
    /// <param name="value">
    /// Valor de <c>autocomplete</c>: <c>username</c>, <c>current-password</c>,
    /// <c>new-password</c>, <c>one-time-code</c>, <c>given-name</c>, <c>family-name</c>.
    /// </param>
    /// <returns>Los atributos para <c>UserAttributes</c>.</returns>
    internal static Dictionary<string, object?> AutoComplete(string value) =>
        new() { ["autocomplete"] = value };
}

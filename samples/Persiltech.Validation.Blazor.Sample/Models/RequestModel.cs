namespace Persiltech.Validation.Blazor.Sample.Models;

/// <summary>
/// Modelo de la página de controles.
/// </summary>
/// <remarks>
/// Una propiedad por cada tipo de entrada que suele llevar un formulario, para ver cómo pinta
/// cada control el mensaje que devuelve la API.
/// <para>
/// <b>Sin anotaciones de datos, a propósito.</b> Aquí todas las comprobaciones se dejan en el
/// servidor —incluso las que el navegador sabría hacer— porque lo que hay que ver es
/// justamente el mensaje que llega de fuera.
/// </para>
/// </remarks>
public sealed class RequestModel
{
    /// <summary>Caja de texto multilínea.</summary>
    public string? Comments { get; set; }

    /// <summary>Desplegable de selección única.</summary>
    public string? Country { get; set; }

    /// <summary>Desplegable de selección múltiple.</summary>
    public List<string> Interests { get; set; } = [];

    /// <summary>Grupo de botones de radio.</summary>
    public string? Plan { get; set; }

    /// <summary>Casilla de verificación.</summary>
    public bool AcceptsTerms { get; set; }

    /// <summary>Interruptor.</summary>
    public bool Newsletter { get; set; }

    /// <summary>Campo numérico.</summary>
    public int? Employees { get; set; }

    /// <summary>Selector de fecha.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Autocompletado.</summary>
    public string? Sector { get; set; }
}

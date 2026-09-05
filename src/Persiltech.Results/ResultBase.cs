namespace Persiltech.Results;

/// <summary>
/// Base común de <see cref="Result"/> y <see cref="Result{TSuccess}"/>: reúne los fallos y
/// responde si la operación salió bien.
/// </summary>
/// <remarks>
/// Un resultado correcto no tiene fallos y uno fallido tiene al menos uno. No hay estado
/// intermedio: <see cref="IsSuccess"/> e <see cref="IsFailure"/> son siempre opuestos.
/// </remarks>
public class ResultBase
{
    /// <summary>
    /// Todos los fallos de la operación. Vacío si salió bien.
    /// </summary>
    public Error[] Errors { get; }

    /// <summary>
    /// El primer fallo, o <see langword="null"/> si no hubo ninguno. Atajo para el caso
    /// habitual de un solo error.
    /// </summary>
    public Error? Error => Errors.Length > 0 ? Errors[0] : null;

    /// <summary>
    /// El mensaje del primer fallo, o <see langword="null"/> si no hubo ninguno.
    /// </summary>
    public string? ErrorMessage => Error?.Message;

    /// <summary>
    /// Indica que la operación falló, es decir, que hay al menos un fallo.
    /// </summary>
    public bool IsFailure => Errors.Length > 0;

    /// <summary>
    /// Indica que la operación salió bien, es decir, que no hay ningún fallo.
    /// </summary>
    public bool IsSuccess => !IsFailure;

    /// <summary>
    /// Inicializa un resultado correcto, sin fallos.
    /// </summary>
    protected ResultBase() => Errors = [];

    /// <summary>
    /// Inicializa un resultado fallido con los fallos indicados.
    /// </summary>
    /// <param name="errors">Fallos de la operación.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> es <c>null</c>.</exception>
    protected ResultBase(Error[] errors)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    /// <summary>
    /// Inicializa un resultado fallido con un único fallo, construido a partir del mensaje.
    /// </summary>
    /// <param name="errorMessage">Mensaje del fallo.</param>
    protected ResultBase(string errorMessage) : this([new Error(errorMessage)])
    { }
}

namespace Persiltech.Results;

/// <summary>
/// Resultado de una operación que no devuelve valor: solo dice si salió bien y, si no, por qué.
/// </summary>
/// <remarks>
/// No se construye con <c>new</c>: se crea con <see cref="Success"/> o con una de las
/// sobrecargas de <see cref="Fail(Error[])"/>, de modo que un resultado siempre nace en uno de
/// los dos estados y nunca a medias.
/// </remarks>
public sealed class Result : ResultBase
{
    private Result() : base() { }
    private Result(Error[] errors) : base(errors) { }
    private Result(string errorMessage) : base(errorMessage) { }

    /// <summary>
    /// Crea un resultado correcto.
    /// </summary>
    public static Result Success() => new();

    /// <summary>
    /// Crea un resultado fallido con los fallos indicados.
    /// </summary>
    /// <param name="errors">Fallos de la operación.</param>
    public static Result Fail(params Error[] errors) => new(errors);

    /// <summary>
    /// Crea un resultado fallido con un único fallo, a partir de su mensaje.
    /// </summary>
    /// <param name="errorMessage">Mensaje del fallo.</param>
    public static Result Fail(string errorMessage) => new(errorMessage);
}

namespace Persiltech.Results;

/// <summary>
/// Resultado de una operación que devuelve un valor cuando sale bien, y fallos cuando no.
/// </summary>
/// <remarks>
/// No se construye con <c>new</c>: se crea con <see cref="Success"/> o con una de las
/// sobrecargas de <see cref="Fail(Error[])"/>.
/// <para>
/// <strong><see cref="Value"/> no está protegida.</strong> En un resultado fallido devuelve el
/// valor predeterminado de <typeparamref name="TSuccess"/> en lugar de lanzar, así que
/// comprueba <see cref="ResultBase.IsSuccess"/> antes de leerla. Es la diferencia con
/// <see cref="Result{TSuccess, TError}"/>, cuya propiedad sí lanza.
/// </para>
/// </remarks>
/// <typeparam name="TSuccess">Tipo del valor que devuelve la operación.</typeparam>
public sealed class Result<TSuccess> : ResultBase
{
    /// <summary>
    /// El valor de la operación, o el valor predeterminado de
    /// <typeparamref name="TSuccess"/> si el resultado es fallido.
    /// </summary>
    public TSuccess Value { get; } = default!;

    private Result(TSuccess value) : base()
    {
        Value = value;
    }

    private Result(Error[] errors) : base(errors) { }
    private Result(string errorMessage) : base(errorMessage) { }

    /// <summary>
    /// Crea un resultado correcto que lleva el valor indicado.
    /// </summary>
    /// <param name="value">Valor que devuelve la operación.</param>
    public static Result<TSuccess> Success(TSuccess value) => new(value);

    /// <summary>
    /// Crea un resultado fallido con los fallos indicados.
    /// </summary>
    /// <param name="errors">Fallos de la operación.</param>
    public static Result<TSuccess> Fail(params Error[] errors) => new(errors);

    /// <summary>
    /// Crea un resultado fallido con un único fallo, a partir de su mensaje.
    /// </summary>
    /// <param name="errorMessage">Mensaje del fallo.</param>
    public static Result<TSuccess> Fail(string errorMessage) => new(errorMessage);
}

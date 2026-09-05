namespace Persiltech.Results.Extensions;

/// <summary>
/// Salidas seguras para <see cref="Result"/>, el resultado sin valor.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Evalúa una de las dos ramas y devuelve un valor. La rama fallida recibe el resultado
    /// entero, del que puede leer <c>Errors</c>.
    /// </summary>
    /// <typeparam name="TResult">Tipo que devuelven ambas ramas.</typeparam>
    /// <param name="result">Resultado que se evalúa.</param>
    /// <param name="onSuccess">Rama que se evalúa si el resultado es correcto.</param>
    /// <param name="onError">Rama que se evalúa si el resultado es fallido.</param>
    public static TResult Match<TResult>(
        this Result result,
        Func<TResult> onSuccess,
        Func<Result, TResult> onError)
    {
        return result.IsSuccess
            ? onSuccess()
            : onError(result);
    }

    /// <summary>
    /// Ejecuta una de las dos ramas, sin devolver nada.
    /// </summary>
    /// <param name="result">Resultado que se evalúa.</param>
    /// <param name="onSuccess">Acción que se ejecuta si el resultado es correcto.</param>
    /// <param name="onError">Acción que se ejecuta si el resultado es fallido.</param>
    public static void Match(
        this Result result,
        Action onSuccess,
        Action<Result> onError)
    {
        if (result.IsSuccess)
            onSuccess();
        else
            onError(result);
    }
}

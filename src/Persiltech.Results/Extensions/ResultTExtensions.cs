namespace Persiltech.Results.Extensions;

/// <summary>
/// Salidas seguras para <see cref="Result{TSuccess}"/>, el resultado con valor.
/// </summary>
/// <remarks>
/// Usar <c>Match</c> en lugar de leer <c>Value</c> directamente evita el punto flojo de este
/// tipo: su propiedad no lanza en un resultado fallido, devuelve el valor predeterminado.
/// </remarks>
public static class ResultTExtensions
{
    /// <summary>
    /// Evalúa una de las dos ramas y devuelve un valor. La rama fallida recibe el resultado
    /// entero, del que puede leer <c>Errors</c>.
    /// </summary>
    /// <typeparam name="T">Tipo del valor del resultado.</typeparam>
    /// <typeparam name="TResult">Tipo que devuelven ambas ramas.</typeparam>
    /// <param name="result">Resultado que se evalúa.</param>
    /// <param name="onSuccess">Rama que recibe el valor si el resultado es correcto.</param>
    /// <param name="onError">Rama que se evalúa si el resultado es fallido.</param>
    public static TResult Match<T, TResult>(
        this Result<T> result,
        Func<T, TResult> onSuccess,
        Func<Result<T>, TResult> onError)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onError(result);
    }

    /// <summary>
    /// Ejecuta una de las dos ramas, sin devolver nada.
    /// </summary>
    /// <typeparam name="T">Tipo del valor del resultado.</typeparam>
    /// <param name="result">Resultado que se evalúa.</param>
    /// <param name="onSuccess">Acción que recibe el valor si el resultado es correcto.</param>
    /// <param name="onError">Acción que se ejecuta si el resultado es fallido.</param>
    public static void Match<T>(
        this Result<T> result,
        Action<T> onSuccess,
        Action<Result<T>> onError)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onError(result);
    }
}

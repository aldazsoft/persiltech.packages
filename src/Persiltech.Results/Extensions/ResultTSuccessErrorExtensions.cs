namespace Persiltech.Results.Extensions;

/// <summary>
/// Salida segura para <see cref="Result{TSuccess, TError}"/>, el resultado con valor en ambas
/// ramas.
/// </summary>
/// <remarks>
/// Hace lo mismo que el <c>Match</c> que el propio tipo ya expone; existe para que las tres
/// variantes de <c>Result</c> se usen igual desde el consumidor.
/// </remarks>
public static class ResultTSuccessErrorExtensions
{
    /// <summary>
    /// Evalúa una de las dos ramas y devuelve un valor.
    /// </summary>
    /// <typeparam name="TSuccess">Tipo del valor cuando sale bien.</typeparam>
    /// <typeparam name="TError">Tipo del valor cuando falla.</typeparam>
    /// <typeparam name="TResult">Tipo que devuelven ambas ramas.</typeparam>
    /// <param name="result">Resultado que se evalúa.</param>
    /// <param name="onSuccess">Rama que recibe el valor correcto.</param>
    /// <param name="onError">Rama que recibe el error.</param>
    public static TResult Match<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, TResult> onSuccess,
        Func<TError, TResult> onError)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onError(result.Error);
    }

    /// <summary>
    /// Ejecuta una de las dos ramas, sin devolver nada.
    /// </summary>
    /// <typeparam name="TSuccess">Tipo del valor cuando sale bien.</typeparam>
    /// <typeparam name="TError">Tipo del valor cuando falla.</typeparam>
    /// <param name="result">Resultado que se evalúa.</param>
    /// <param name="onSuccess">Acción que recibe el valor correcto.</param>
    /// <param name="onError">Acción que recibe el error.</param>
    public static void Match<TSuccess, TError>(
        this Result<TSuccess, TError> result,
        Action<TSuccess> onSuccess,
        Action<TError> onError)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onError(result.Error);
    }
}

namespace Persiltech.Results;

/// <summary>
/// Resultado de una operación que devuelve un valor propio en cada rama: uno cuando sale bien
/// y otro cuando falla, ambos con su tipo.
/// </summary>
/// <remarks>
/// Es la variante para programación ferroviaria: <see cref="Map"/>, <see cref="Bind"/> y sus
/// compañeros encadenan operaciones y cortocircuitan en cuanto una falla, sin que el llamador
/// escriba una sola comprobación intermedia.
/// <para>
/// A diferencia de <see cref="Result{TSuccess}"/>, aquí <see cref="Value"/> y
/// <see cref="Error"/> <strong>lanzan</strong> si se leen en la rama que no corresponde. Es
/// deliberado: leer el valor de un resultado fallido es un error de programación, no un caso
/// que valga la pena representar.
/// </para>
/// </remarks>
/// <typeparam name="TSuccess">Tipo del valor cuando la operación sale bien.</typeparam>
/// <typeparam name="TError">Tipo del valor cuando la operación falla.</typeparam>
public sealed class Result<TSuccess, TError>
{
    private readonly TSuccess SuccessValue;
    private readonly TError ErrorValue;

    /// <summary>
    /// El valor de la rama correcta.
    /// </summary>
    /// <exception cref="InvalidOperationException">El resultado es fallido.</exception>
    public TSuccess Value => IsSuccess
        ? SuccessValue
        : throw new InvalidOperationException(
            ResultMessages.CannotAccessValueWhenResultIsFailureMessage);

    /// <summary>
    /// El valor de la rama fallida.
    /// </summary>
    /// <exception cref="InvalidOperationException">El resultado es correcto.</exception>
    public TError Error => IsFailure
        ? ErrorValue
        : throw new InvalidOperationException(
            ResultMessages.CannotAccessErrorWhenResultIsSuccess);

    /// <summary>
    /// Indica que la operación salió bien.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indica que la operación falló.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    private Result(TSuccess value)
    {
        IsSuccess = true;
        SuccessValue = value;
        ErrorValue = default!;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        ErrorValue = error;
        SuccessValue = default!;
    }

    // ── Factory ────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un resultado correcto con el valor indicado.
    /// </summary>
    /// <param name="successValue">Valor de la rama correcta.</param>
    public static Result<TSuccess, TError> Success(TSuccess successValue) => new(successValue);

    /// <summary>
    /// Crea un resultado fallido con el valor indicado.
    /// </summary>
    /// <param name="errorValue">Valor de la rama fallida.</param>
    public static Result<TSuccess, TError> Fail(TError errorValue) => new(errorValue);

    // ── Implicit operators ─────────────────────────────────────────────────

    /// <summary>
    /// Convierte un valor en un resultado correcto, para poder devolverlo sin nombrar el tipo.
    /// </summary>
    /// <remarks>
    /// Con <typeparamref name="TSuccess"/> y <typeparamref name="TError"/> del mismo tipo la
    /// conversión sería ambigua, así que en ese caso usa <see cref="Success"/> o
    /// <see cref="Fail"/>.
    /// </remarks>
    /// <param name="value">Valor de la rama correcta.</param>
    public static implicit operator Result<TSuccess, TError>(TSuccess value) => Success(value);

    /// <summary>
    /// Convierte un error en un resultado fallido, para poder devolverlo sin nombrar el tipo.
    /// </summary>
    /// <param name="error">Valor de la rama fallida.</param>
    public static implicit operator Result<TSuccess, TError>(TError error) => Fail(error);

    // ── Map ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Transforma el valor de éxito. Si es failure, propaga el error sin cambios.
    /// </summary>
    /// <typeparam name="TNew">Tipo del valor transformado.</typeparam>
    /// <param name="mapper">Transformación que se aplica al valor correcto.</param>
    /// <returns>Un resultado con el valor transformado, o el mismo error.</returns>
    public Result<TNew, TError> Map<TNew>(Func<TSuccess, TNew> mapper) =>
        IsSuccess
            ? Result<TNew, TError>.Success(mapper(SuccessValue))
            : Result<TNew, TError>.Fail(ErrorValue);

    /// <summary>
    /// Transforma el error. Si es success, propaga el valor sin cambios.
    /// </summary>
    /// <typeparam name="TNewError">Tipo del error transformado.</typeparam>
    /// <param name="mapper">Transformación que se aplica al error.</param>
    /// <returns>Un resultado con el error transformado, o el mismo valor.</returns>
    public Result<TSuccess, TNewError> MapError<TNewError>(Func<TError, TNewError> mapper) =>
        IsFailure
            ? Result<TSuccess, TNewError>.Fail(mapper(ErrorValue))
            : Result<TSuccess, TNewError>.Success(SuccessValue);

    // ── Bind ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Encadena una operación que también retorna Result (railway-oriented programming).
    /// Si es failure, cortocircuita y propaga el error.
    /// </summary>
    /// <typeparam name="TNew">Tipo del valor que devuelve la operación encadenada.</typeparam>
    /// <param name="binder">Operación que se ejecuta solo si este resultado es correcto.</param>
    /// <returns>El resultado de la operación encadenada, o el error original.</returns>
    public Result<TNew, TError> Bind<TNew>(Func<TSuccess, Result<TNew, TError>> binder) =>
        IsSuccess
            ? binder(SuccessValue)
            : Result<TNew, TError>.Fail(ErrorValue);

    /// <summary>
    /// Versión async de <see cref="Bind"/>.
    /// </summary>
    /// <typeparam name="TNew">Tipo del valor que devuelve la operación encadenada.</typeparam>
    /// <param name="binder">Operación asíncrona que se ejecuta solo si este resultado es correcto.</param>
    /// <returns>El resultado de la operación encadenada, o el error original.</returns>
    public async Task<Result<TNew, TError>> BindAsync<TNew>(
        Func<TSuccess, Task<Result<TNew, TError>>> binder) =>
        IsSuccess
            ? await binder(SuccessValue)
            : Result<TNew, TError>.Fail(ErrorValue);

    // ── Tap (side-effects) ─────────────────────────────────────────────────

    /// <summary>
    /// Ejecuta una acción si es success, sin transformar el resultado.
    /// Útil para logging o auditoría.
    /// </summary>
    /// <param name="action">Acción que recibe el valor correcto.</param>
    /// <returns>El mismo resultado, para poder encadenar.</returns>
    public Result<TSuccess, TError> OnSuccess(Action<TSuccess> action)
    {
        if (IsSuccess) action(SuccessValue);
        return this;
    }

    /// <summary>
    /// Ejecuta una acción si es failure, sin transformar el resultado.
    /// Útil para logging de errores.
    /// </summary>
    /// <param name="action">Acción que recibe el error.</param>
    /// <returns>El mismo resultado, para poder encadenar.</returns>
    public Result<TSuccess, TError> OnFailure(Action<TError> action)
    {
        if (IsFailure) action(ErrorValue);
        return this;
    }

    // ── Match ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Evalúa uno de los dos branches y retorna un valor.
    /// </summary>
    /// <remarks>
    /// Es la forma segura de salir del <c>Result</c>: obliga a contemplar ambas ramas, así que
    /// no hay manera de leer el valor equivocado.
    /// </remarks>
    /// <typeparam name="TResult">Tipo que devuelven ambas ramas.</typeparam>
    /// <param name="onSuccess">Rama que se evalúa si el resultado es correcto.</param>
    /// <param name="onError">Rama que se evalúa si el resultado es fallido.</param>
    public TResult Match<TResult>(
        Func<TSuccess, TResult> onSuccess,
        Func<TError, TResult> onError) =>
        IsSuccess ? onSuccess(SuccessValue) : onError(ErrorValue);

    /// <summary>
    /// Versión void de Match para ejecutar side-effects.
    /// </summary>
    /// <param name="onSuccess">Acción que se ejecuta si el resultado es correcto.</param>
    /// <param name="onError">Acción que se ejecuta si el resultado es fallido.</param>
    public void Match(Action<TSuccess> onSuccess, Action<TError> onError)
    {
        if (IsSuccess) onSuccess(SuccessValue);
        else onError(ErrorValue);
    }
}

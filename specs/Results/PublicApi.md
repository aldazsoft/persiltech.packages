---
packageName: Persiltech.Results
version: 1.0.1
---

# Propósito

Declarar la superficie pública de `Persiltech.Results` tal como está implementada.

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera este flujo, así
> que esta especificación no precedió al código: se levantó leyéndolo al homologar el
> paquete. Documenta lo que hay, no un diseño pendiente.

# Superficie pública

El paquete ofrece **tres formas de resultado**, según lo que la operación tenga que devolver.

## `Persiltech.Results`

```csharp
public class Error(string? code, string message)
{
    public string? Code { get; }
    public string Message { get; }

    public Error(string message);
}

public class ResultBase
{
    public Error[] Errors { get; }
    public Error? Error { get; }
    public string? ErrorMessage { get; }
    public bool IsFailure { get; }
    public bool IsSuccess { get; }

    protected ResultBase();
    protected ResultBase(Error[] errors);
    protected ResultBase(string errorMessage);
}

// Sin valor.
public sealed class Result : ResultBase
{
    public static Result Success();
    public static Result Fail(params Error[] errors);
    public static Result Fail(string errorMessage);
}

// Con valor solo en la rama correcta.
public sealed class Result<TSuccess> : ResultBase
{
    public TSuccess Value { get; }

    public static Result<TSuccess> Success(TSuccess value);
    public static Result<TSuccess> Fail(params Error[] errors);
    public static Result<TSuccess> Fail(string errorMessage);
}

// Con valor propio en cada rama. Es la variante ferroviaria.
public sealed class Result<TSuccess, TError>
{
    public TSuccess Value { get; }   // lanza si el resultado es fallido
    public TError Error { get; }     // lanza si el resultado es correcto
    public bool IsSuccess { get; }
    public bool IsFailure { get; }

    public static Result<TSuccess, TError> Success(TSuccess successValue);
    public static Result<TSuccess, TError> Fail(TError errorValue);

    public static implicit operator Result<TSuccess, TError>(TSuccess value);
    public static implicit operator Result<TSuccess, TError>(TError error);

    public Result<TNew, TError> Map<TNew>(Func<TSuccess, TNew> mapper);
    public Result<TSuccess, TNewError> MapError<TNewError>(Func<TError, TNewError> mapper);

    public Result<TNew, TError> Bind<TNew>(Func<TSuccess, Result<TNew, TError>> binder);
    public Task<Result<TNew, TError>> BindAsync<TNew>(Func<TSuccess, Task<Result<TNew, TError>>> binder);

    public Result<TSuccess, TError> OnSuccess(Action<TSuccess> action);
    public Result<TSuccess, TError> OnFailure(Action<TError> action);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onError);
    public void Match(Action<TSuccess> onSuccess, Action<TError> onError);
}
```

## `Persiltech.Results.Extensions`

Un `Match` por cada forma de resultado, para que las tres se consuman igual.

```csharp
public static class ResultExtensions
{
    public static TResult Match<TResult>(this Result result, Func<TResult> onSuccess, Func<Result, TResult> onError);
    public static void Match(this Result result, Action onSuccess, Action<Result> onError);
}

public static class ResultTExtensions
{
    public static TResult Match<T, TResult>(this Result<T> result, Func<T, TResult> onSuccess, Func<Result<T>, TResult> onError);
    public static void Match<T>(this Result<T> result, Action<T> onSuccess, Action<Result<T>> onError);
}

public static class ResultTSuccessErrorExtensions
{
    public static TResult Match<TSuccess, TError, TResult>(this Result<TSuccess, TError> result, Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onError);
    public static void Match<TSuccess, TError>(this Result<TSuccess, TError> result, Action<TSuccess> onSuccess, Action<TError> onError);
}
```

## `Persiltech.Results.Resources`

```csharp
public class ResultMessages
{
    public static string CannotAccessErrorWhenResultIsSuccess { get; }
    public static string CannotAccessValueWhenResultIsFailureMessage { get; }
}
```

# Decisiones de diseño

- **Los resultados no se construyen con `new`**: solo con `Success` o `Fail`. Así nacen siempre
  en uno de los dos estados y nunca a medias.
- **`Result<TSuccess, TError>` lanza al leer la rama equivocada**, con mensajes localizados por
  `Persiltech.Localizer`. Es deliberado: leer el valor de un resultado fallido es un error de
  programación, no un caso que valga la pena representar.
- **Las conversiones implícitas** permiten devolver un valor o un error sin nombrar el tipo del
  resultado.
- **`Map`, `Bind`, `OnSuccess` y `OnFailure` cortocircuitan**: encadenan operaciones y en cuanto
  una falla, el resto se salta sin una sola comprobación intermedia del llamador.

# Fuera de alcance

- Registro en el contenedor de dependencias: no hay servicios que registrar.
- Convertir excepciones en resultados, o al revés. Eso lo decide el consumidor.
- Agregar resultados de varias operaciones en uno solo.

# Deuda conocida

- **`Result<TSuccess>.Value` no está protegida.** En un resultado fallido devuelve el valor
  predeterminado en lugar de lanzar, al contrario que su hermana de dos parámetros. Un
  consumidor que olvide comprobar `IsSuccess` recibe un `null` o un cero en silencio. Los
  `Match` de las extensiones existen en parte para esquivarlo.
- **Las dos jerarquías no comparten raíz**: `Result` y `Result<TSuccess>` heredan de
  `ResultBase`, pero `Result<TSuccess, TError>` no. No hay un tipo común que las abarque.
- **`Map` y `MapError` no capturan excepciones** de la función que reciben: si la
  transformación lanza, la excepción atraviesa el `Result`.
- **`ResultBase` no es `abstract`** aunque sus tres constructores sean `protected`.

Corregir cualquiera cambia la superficie o el comportamiento, así que ninguna se tocó al
homologar.

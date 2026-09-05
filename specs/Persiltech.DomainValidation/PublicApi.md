---
packageName: Persiltech.DomainValidation
version: 2.0.2
---

# Propósito

Declarar la superficie pública de `Persiltech.DomainValidation` tal como está implementada.

> **Nota sobre este archivo.** El paquete se escribió a mano, así que esta especificación no
> precedió al código: se levantó leyéndolo al homologar el repositorio. Por eso documenta lo
> que hay, no un diseño pendiente de implementar. La `2.0.0` es la primera versión que cambia
> el contrato; sus rupturas están recogidas en la guía de migración del README.

# Superficie pública

## `Persiltech.DomainValidation.Interfaces`

```csharp
public interface ISpecification<T>
{
    ValueTask<IReadOnlyList<SpecificationError>> EvaluateAsync(
        T entity, CancellationToken cancellationToken = default);
}

public interface IDomainSpecification<T>
{
    bool EvaluateOnlyIfNoPreviousErrors { get; }
    bool StopOnFirstEntitySpecificationError { get; }

    Task<IReadOnlyList<SpecificationError>> ValidateAsync(
        T entity, CancellationToken cancellationToken = default);
}

public interface IDomainSpecificationsValidator<T>
{
    Task<ValidationResult> ValidateAsync(
        T entity, CancellationToken cancellationToken = default);
}

public interface IPropertySpecificationsTree<T>
{
    string PropertyName { get; }
    IReadOnlyList<ISpecification<T>> Specifications { get; }
    bool StopOnFirstPropertySpecificationError { get; }
}
```

## `Persiltech.DomainValidation.ValueObjects`

```csharp
public class SpecificationError(string propertyName, string errorMessage)
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }
}

public class ValidationResult(IEnumerable<SpecificationError> errors)
{
    public IReadOnlyList<SpecificationError> Errors { get; }
    public bool IsValid { get; }
}
```

## `Persiltech.DomainValidation.Core`

```csharp
public abstract class DomainSpecificationBase<T>(
    bool evaluateOnlyIfNoPreviousErrors = false,
    bool stopOnFirstEntitySpecificationError = false) : IDomainSpecification<T>
{
    protected PropertySpecificationsTree<T, TProperty> Property<TProperty>(
        Expression<Func<T, TProperty>> propertyExpression,
        bool stopOnFirstPropertySpecificationError = false);

    protected virtual Task<List<SpecificationError>> ValidateSpecificationsAsync(
        T entity, CancellationToken cancellationToken = default);
}

public class Specification<T>(Func<T, IEnumerable<SpecificationError>> validationRule)
    : ISpecification<T>;

public class AsyncSpecification<T>(
    Func<T, CancellationToken, ValueTask<IEnumerable<SpecificationError>>> validationRule)
    : ISpecification<T>;

public class PropertySpecificationsTree<T, TProperty>(
    Expression<Func<T, TProperty>> propertyExpression,
    bool stopOnFirstPropertySpecificationError = false) : IPropertySpecificationsTree<T>
{
    public TProperty GetPropertyValue(T entity);

    public PropertySpecificationsTree<T, TProperty> Add(ISpecification<T> specification);
}

public class DomainSpecificationsValidator<T>(IEnumerable<IDomainSpecification<T>> specifications)
    : IDomainSpecificationsValidator<T>;
```

## `Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions`

Las reglas fluidas. Todas devuelven el mismo árbol para poder encadenarlas, y todas aceptan
un `errorMessage` que sustituye al mensaje predeterminado.

```csharp
IsRequired<T, TProperty>(string? errorMessage = null)
NotEmpty<T, TProperty>(string? errorMessage = null)
HasMinLength<T>(int minLength, string? errorMessage = null)
HasMaxLength<T>(int maxLength, string? errorMessage = null)
HasFixedLength<T>(int length, string? errorMessage = null)
EmailAddress<T>(string? errorMessage = null)
Matches<T>(string regularExpression, string? errorMessage = null)
Equal<T, TProperty>(TProperty comparisonValue, string? errorMessage = null)
Equal<T, TProperty>(Expression<Func<T, TProperty>> comparisonProperty, string? errorMessage = null)
GreaterThan<T, TProperty>(TProperty comparisonValue, string? errorMessage = null)
GreaterThanOrEqualTo<T, TProperty>(TProperty comparisonValue, string? errorMessage = null)
Must<T, TProperty>(Func<T, bool> predicate, string errorMessage)
Must<T, TProperty>(Func<TProperty, bool> predicate, string errorMessage)
MustAsync<T, TProperty>(Func<T, CancellationToken, ValueTask<bool>> predicate, string errorMessage)
SetValidator<T, TElement>(IDomainSpecificationsValidator<TElement> validator)
```

Sobrecargas para propiedades anulables por valor, sobre
`PropertySpecificationsTree<T, TProperty?>` con `TProperty : struct, IComparable<TProperty>`:

```csharp
Equal<T, TProperty>(TProperty? comparisonValue, string? errorMessage = null)
Equal<T, TProperty>(Expression<Func<T, TProperty?>> comparisonProperty, string? errorMessage = null)
GreaterThan<T, TProperty>(TProperty comparisonValue, string? errorMessage = null)
GreaterThanOrEqualTo<T, TProperty>(TProperty comparisonValue, string? errorMessage = null)
```

## `Persiltech.DomainValidation.Guards` y `.Exceptions`

```csharp
public static class DomainValidationGuard
{
    public static Task AgainstInvalidSpecification<T>(
        IDomainSpecificationsValidator<T> validator, T entity, string? message = null,
        CancellationToken cancellationToken = default);
}

public class DomainValidationException : Exception
{
    public IReadOnlyList<SpecificationError>? Errors { get; }
}
```

## `Persiltech.DomainValidation.Resources`

```csharp
public sealed class ErrorMessages
{
    public static string EmailAddress { get; }
    public static string Equal { get; }
    public static string GreaterThan { get; }
    public static string GreaterThanOrEqualTo { get; }
    public static string HasFixedLength { get; }
    public static string HasMaxLength { get; }
    public static string HasMinLength { get; }
    public static string IsRequired { get; }
    public static string Matches { get; }
    public static string NotEmpty { get; }
}
```

No es una clase estática porque `Persiltech.Localizer` la usa como argumento de tipo genérico
para localizar el recurso, y un tipo estático no puede serlo. El constructor privado deja el
efecto práctico: nadie la instancia.

## `Persiltech.DomainValidation`

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddDomainSpecificationsValidator(this IServiceCollection services);
}
```

# Decisiones de diseño

- La evaluación **no deja estado** en la regla ni en la especificación: devuelven sus errores
  en lugar de guardarlos en una propiedad. Es lo que permite que una misma instancia evalúe
  varias entidades a la vez, y lo que hace que el tiempo de vida con que se registre deje de
  ser una trampa.
- El recorrido es **asíncrono de extremo a extremo** y lleva el `CancellationToken` hasta la
  última regla, incluidas las de cada elemento dentro de `SetValidator`. Las reglas que se
  resuelven en memoria usan `ValueTask`, de modo que no asignan al completarse de forma
  síncrona.
- El registro en el contenedor es de un **genérico abierto** con tiempo de vida `Scoped`, y
  usa `TryAddScoped`: llamarlo dos veces no duplica el servicio. Las especificaciones concretas
  las registra la aplicación consumidora, no el paquete.
- El validador separa las especificaciones **incondicionales** de las **condicionales**
  (`EvaluateOnlyIfNoPreviousErrors`). Las condicionales solo se evalúan si ninguna
  incondicional produjo errores, y se detienen en la primera que falle: es lo que evita
  golpear la base de datos mientras el formato del dato siga siendo inválido.
- El tratamiento del valor ausente **no es uniforme entre reglas**, y es deliberado:
  `HasMaxLength` da por bueno un `null` (no hay longitud que exceder), mientras que
  `HasMinLength` y `HasFixedLength` lo rechazan.
- `GreaterThan` y `GreaterThanOrEqualTo` rechazan un `null`: no hay nada que pueda superar al
  valor de comparación. Sus sobrecargas para `struct` cubren los tipos anulables por valor, que
  la restricción `IComparable<T>` no admite.
- `Property` exige una expresión que seleccione un miembro y lanza `ArgumentException` si no lo
  hace: sin nombre de propiedad, el error no tendría a qué atribuirse.
- `Matches` analiza la expresión regular al declarar la regla y limita cada evaluación a un
  segundo, para que un patrón de retroceso catastrófico no cuelgue el hilo.
- `ValidationResult` materializa los errores al construirse: quien lo recibe consulta `IsValid`
  y `Errors` varias veces, y una secuencia perezosa se recorrería entera en cada consulta.
- Los mensajes predeterminados se resuelven con `Persiltech.Localizer`, de modo que salen en el
  idioma de la aplicación consumidora. Hay dos traducciones: la neutra, en inglés, y la
  española, que cubre toda la familia `es-*`; cualquier otra cultura cae en la neutra.

# Fuera de alcance

- Validación de entrada de la capa de presentación: esto valida reglas de **negocio**.
- Autenticación y autorización.
- Reglas configurables por archivo o por atributos: las especificaciones son clases.

# Compatibilidad con versiones publicadas

La última versión en nuget.org es la `2.0.2`, la misma que declara el `.csproj`: la superficie
descrita arriba es la que está publicada.

La `2.0.0` rompió a propósito el contrato de la `1.0.x`, y la guía de migración del README
recoge cada ruptura. Las `2.0.1` y `2.0.2` no tocaron el código —corrigieron el historial del
README y renovaron el icono del paquete—, así que la superficie no cambia desde la `2.0.0`.

La comprobación es manual: el repositorio no declara una línea base contra la que comparar la
superficie.

La deuda que la `1.0.x` dejaba anotada quedó saldada en la `2.0.0`: `HasFixedLenghtSpecification`
pasó a `HasFixedLengthExtension`, `HasMinLenghtExtension` a `HasMinLengthExtension` y
`DependencyContainer` a `DependencyInjection`. Ninguno afectaba al consumidor, que invoca los
métodos de extensión sin nombrar la clase, salvo el último.

# Clean Architecture: DomainValidation

[![NuGet](https://img.shields.io/nuget/v/Persiltech.DomainValidation.svg)](https://www.nuget.org/packages/Persiltech.DomainValidation/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Mantiene las reglas de negocio en el dominio, fuera de los controladores y de los manejadores
de casos de uso. Cada regla es una *especificación*: una clase que declara qué debe cumplir una
entidad. Un validador las evalúa todas y devuelve los errores reunidos en un `ValidationResult`,
sin lanzar excepciones salvo que se le pida.

Desarrollada durante el entrenamiento
[Introducción a Clean Architecture en aplicaciones .NET](https://ticapacitacion.com/curso/introca/),
impartido por Miguel Muñoz Serafín.

## Instalación

    dotnet add package Persiltech.DomainValidation

Arrastra dos dependencias: `Persiltech.Localizer`, que resuelve los mensajes de error
predeterminados, y `Microsoft.Extensions.DependencyInjection.Abstractions`, que aporta el
registro en el contenedor.

## El contrato

La superficie se reparte en varios espacios de nombres. El de las reglas fluidas es el menos
evidente, y sin él el compilador no ve `IsRequired` ni sus hermanas:

```csharp
using Persiltech.DomainValidation;
using Persiltech.DomainValidation.Core;
using Persiltech.DomainValidation.Exceptions;
using Persiltech.DomainValidation.Extensions.PropertySpecificationTreeExtensions;
using Persiltech.DomainValidation.Guards;
using Persiltech.DomainValidation.Interfaces;
using Persiltech.DomainValidation.ValueObjects;
```

Los tipos que se usan a diario:

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

public interface IDomainSpecificationsValidator<T>
{
    Task<ValidationResult> ValidateAsync(T entity, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<SpecificationError> Errors { get; }
}

public class SpecificationError(string propertyName, string errorMessage)
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddDomainSpecificationsValidator(this IServiceCollection services);
}
```

La evaluación **no deja estado** en la especificación: devuelve sus errores en lugar de
guardarlos en una propiedad. Por eso una misma instancia puede validar varias entidades a la
vez, y el tiempo de vida con que la registres deja de ser una trampa.

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
```

### Reglas disponibles

Todas devuelven el mismo árbol para poder encadenarlas, y todas aceptan un `errorMessage` que
sustituye al predeterminado.

| Regla | Qué comprueba |
| --- | --- |
| `IsRequired(errorMessage)` | Rechaza `null` y, en una cadena, el texto vacío o formado solo por espacios |
| `NotEmpty(errorMessage)` | Rechaza `null` y la colección sin elementos |
| `HasMinLength(minLength, errorMessage)` | Longitud mínima de una cadena. Un `null` se rechaza |
| `HasMaxLength(maxLength, errorMessage)` | Longitud máxima de una cadena. Un `null` se da por bueno |
| `HasFixedLength(length, errorMessage)` | Longitud exacta de una cadena. Un `null` se rechaza |
| `EmailAddress(errorMessage)` | Que la cadena tenga forma de dirección de correo |
| `Matches(regularExpression, errorMessage)` | Que la cadena case con la expresión regular. Un `null` se rechaza |
| `Equal(comparisonValue, errorMessage)` | Igualdad con un valor fijo. Dos `null` se consideran iguales |
| `Equal(comparisonProperty, errorMessage)` | Igualdad con otra propiedad de la entidad, como una confirmación de contraseña |
| `GreaterThan(comparisonValue, errorMessage)` | Valor estrictamente mayor. Un `null` se rechaza |
| `GreaterThanOrEqualTo(comparisonValue, errorMessage)` | Valor mayor o igual. Un `null` se rechaza |
| `Must(predicate, errorMessage)` | Regla propia, sobre la entidad completa o sobre el valor de la propiedad |
| `MustAsync(predicate, errorMessage)` | Regla propia que va a la base de datos o a un servicio |
| `SetValidator(validator)` | Aplica el validador de otro tipo a cada elemento de una colección |

El tratamiento del valor ausente **no es uniforme**, y es deliberado: `HasMaxLength` da por
buena una propiedad `null` porque no hay longitud que exceder, mientras que `HasMinLength` y
`HasFixedLength` la rechazan.

`GreaterThan` y `GreaterThanOrEqualTo` rechazan un valor ausente: no hay nada que pueda superar
al valor de comparación. Si la propiedad también es obligatoria y prefieres un solo error en
lugar de dos, precédelas de `IsRequired` y abre el árbol con
`stopOnFirstPropertySpecificationError: true`, para que la comparación no llegue a ejecutarse
cuando el valor falta.

`GreaterThan`, `GreaterThanOrEqualTo` y `Equal` traen sobrecargas para propiedades anulables
por valor, que la restricción `IComparable<T>` no admite por sí sola:

```csharp
Property(a => a.Quantity).GreaterThan(0);          // int?
Property(a => a.UnitPrice).GreaterThanOrEqualTo(0m); // decimal?
Property(a => a.Quantity).Equal(a => a.ConfirmedQuantity);
```

`Matches` analiza la expresión regular al declarar la regla, no en cada validación, y limita
cada evaluación a un segundo: un patrón de retroceso catastrófico falla con
`RegexMatchTimeoutException` en vez de colgar el hilo.

## Uso

Una especificación hereda de `DomainSpecificationBase<T>` y declara sus reglas en el constructor:

```csharp
public class UserRegistrationSpecification : DomainSpecificationBase<UserRegistration>
{
    public UserRegistrationSpecification()
    {
        Property(u => u.Email, stopOnFirstPropertySpecificationError: true)
            .IsRequired()
            .EmailAddress();

        Property(u => u.Password)
            .IsRequired()
            .HasMinLength(6)
            .Matches("[A-Z]", "Se requieren caracteres en mayúscula.");

        Property(u => u.ConfirmPassword)
            .Equal(u => u.Password, "La confirmación no coincide con la contraseña.");
    }
}
```

`AddDomainSpecificationsValidator` registra el validador como genérico abierto y con tiempo de
vida `Scoped`. Las especificaciones las registra la aplicación:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDomainSpecificationsValidator();

builder.Services.AddScoped<IDomainSpecification<UserRegistration>,
    UserRegistrationSpecification>();

var app = builder.Build();
```

Y la capa de aplicación depende solo del validador:

```csharp
public sealed class RegisterUserHandler(
    IDomainSpecificationsValidator<UserRegistration> validator)
{
    public async Task<ValidationResult> HandleAsync(UserRegistration registration)
    {
        var result = await validator.ValidateAsync(registration);

        if (!result.IsValid)
        {
            return result;
        }

        // La entidad ya cumple sus reglas de negocio.
        return result;
    }
}
```

### Orden de evaluación

Hay tres interruptores, y cada uno actúa en un nivel distinto:

1. `Property(expresión, stopOnFirstPropertySpecificationError: true)` detiene las reglas *de esa
   propiedad* en cuanto una falla, para no acumular errores que se explican entre sí.
2. `DomainSpecificationBase<T>(stopOnFirstEntitySpecificationError: true)` detiene el recorrido
   de las demás propiedades *de esa especificación* cuando ya hay algún error.
3. `EvaluateOnlyIfNoPreviousErrors` marca una especificación como condicional. El validador
   evalúa primero todas las incondicionales; solo si ninguna produjo errores pasa a las
   condicionales, y ahí se detiene en la primera que falle.

El tercero es el que evita el trabajo caro. Una comprobación contra la base de datos no tiene
sentido mientras el formato del dato siga siendo inválido:

```csharp
public class UniqueEmailSpecification(IUserRepository users)
    : DomainSpecificationBase<UserRegistration>(evaluateOnlyIfNoPreviousErrors: true)
{
    protected override async Task<List<SpecificationError>> ValidateSpecificationsAsync(
        UserRegistration entity, CancellationToken cancellationToken = default)
    {
        List<SpecificationError> errors = [];

        if (await users.ExistsByEmailAsync(entity.Email, cancellationToken))
        {
            errors.Add(new SpecificationError(
                nameof(UserRegistration.Email), "El correo indicado ya existe."));
        }

        return errors;
    }
}
```

Cuando la comprobación cabe en un predicado, `MustAsync` evita sobrescribir el método:

```csharp
Property(u => u.Email).MustAsync(
    async (entity, cancellationToken) =>
        !await users.ExistsByEmailAsync(entity.Email, cancellationToken),
    "El correo indicado ya existe.");
```

### Validar colecciones

`SetValidator` aplica el validador de otro tipo a cada elemento, de forma asíncrona y sin
bloquear ningún hilo, de modo que las reglas del elemento pueden ir a la base de datos como
cualquier otra. Los errores llegan con la ruta del elemento incorporada al nombre de la
propiedad —`OrderDetails[0].Quantity`—, así que el consumidor sabe qué fila falló:

```csharp
public class CreateOrderSpecification : DomainSpecificationBase<CreateOrder>
{
    public CreateOrderSpecification(
        IDomainSpecificationsValidator<CreateOrderDetail> orderDetailsValidator)
    {
        Property(o => o.CustomerId)
            .IsRequired()
            .HasFixedLength(5);

        Property(o => o.OrderDetails)
            .NotEmpty();

        Property(o => o.OrderDetails)
            .SetValidator(orderDetailsValidator);
    }
}
```

### Cortar el flujo con una guarda

Cuando el caso de uso no tiene nada que hacer con una entidad inválida, `DomainValidationGuard`
valida y lanza `DomainValidationException` con los errores en su propiedad `Errors`:

```csharp
await DomainValidationGuard.AgainstInvalidSpecification(
    validator, registration, "El registro del usuario no es válido.");
```

### Mensajes de error

Los mensajes predeterminados viven en `ErrorMessages` y los resuelve `Persiltech.Localizer`, así
que salen en el idioma de la aplicación. Los que llevan un dato —longitud, valor de comparación,
expresión regular— se componen con `string.Format`. Para un mensaje propio, pásalo como último
argumento de la regla; en `Must` es obligatorio, porque no hay predeterminado que aplicar.

El paquete trae dos traducciones: la **neutra**, en inglés, y la **española**, que cubre toda la
familia `es-*`. Cualquier otra cultura cae en la neutra, de modo que el usuario final siempre ve
un mensaje y nunca la clave del recurso.

## Decisiones de diseño

- La evaluación no deja estado: cada regla devuelve sus errores en lugar de guardarlos. Es lo
  que hace que una especificación pueda validar varias entidades a la vez.
- El recorrido es asíncrono de extremo a extremo y lleva el `CancellationToken` hasta la última
  regla, incluidas las de cada elemento dentro de `SetValidator`.
- El registro en el contenedor es de un genérico abierto con `TryAddScoped`: llamarlo dos veces
  no duplica el servicio. Las especificaciones concretas las registra la aplicación consumidora.
- El validador separa las especificaciones incondicionales de las condicionales, y solo evalúa
  estas últimas si ninguna anterior produjo errores.
- El tratamiento del valor ausente no es uniforme entre reglas, y es deliberado.
- Los mensajes predeterminados se localizan, de modo que salen en el idioma de la aplicación.

### Fuera de alcance

- Validación de entrada de la capa de presentación: esto valida reglas de **negocio**.
- Autenticación y autorización.
- Reglas configurables por archivo o por atributos: las especificaciones son clases.

## Compatibilidad

`net10.0`

## Historial de versiones

El código fuente vive en el [monorepo](https://github.com/aldazsoft/persiltech.packages); esta tabla resume qué cambió en cada versión publicada.

| Versión | Cambios                                                                                     |
| ------- | ------------------------------------------------------------------------------------------- |
| 2.0.3   | El `.nuspec` declara el repositorio, ahora público, y se activa SourceLink: el depurador del consumidor puede entrar al código fuente. El README enlaza al monorepo y el soporte pasa a las incidencias de GitHub. El suelo de `Persiltech.Localizer` sube de 1.0.1 a 1.0.3. Sin cambios en el código ni en la superficie pública. |
| 2.0.2   | Renueva el icono del paquete, que es lo único que cambia de cara al consumidor: pesa la mitad (12 401 → 6 575 bytes) con la misma resolución de 128 × 128. Sin cambios en el código ni en la superficie pública. |
| 2.0.1   | Corrige este historial, que listaba una `1.0.2` y una `1.0.3` que se prepararon pero nunca llegaron a nuget.org. Sin cambios en el código ni en la superficie pública. |
| 2.0.0   | La evaluación deja de guardar estado, de modo que una especificación compartida ya no devuelve el veredicto de otra entidad. El recorrido pasa a ser asíncrono de extremo a extremo y acepta `CancellationToken`; `SetValidator` deja de bloquear. Nuevas `MustAsync` y `AsyncSpecification`. Sobrecargas de comparación para propiedades anulables por valor. `ValidationResult.Errors` pasa a `IReadOnlyList`. `DependencyContainer` se renombra a `DependencyInjection` y se corrigen las erratas `HasFixedLenghtSpecification` y `HasMinLenghtExtension`. **Ver la guía de migración.** Trae además el empaquetado corregido —licencia dentro del `.nupkg`, página del proyecto, README publicable— y las correcciones de localización y de comparaciones contra `null` que se prepararon como `1.0.2` y `1.0.3` sin publicarse. |
| 1.0.1   | Primera versión disponible en nuget.org; reemplaza a la 1.0.0, retirada del listado.        |

Actualizar dentro de la rama `1.0.x` siempre fue seguro: la superficie no cambió. La `2.0.0` sí
rompe el contrato, y a propósito.

### Migrar de 1.0.x a 2.0.0

El cambio de fondo es que **la evaluación deja de guardar estado**. Antes se preguntaba
"¿es válida?" y luego se leía la propiedad `Errors`; ahora la evaluación *devuelve* los errores.
De ahí sale casi todo lo demás.

| Antes (`1.0.x`) | Ahora (`2.0.0`) |
| --- | --- |
| `bool ok = await specification.ValidateAsync(entity);`<br>`var errors = specification.Errors;` | `var errors = await specification.ValidateAsync(entity);`<br>`bool ok = errors.Count == 0;` |
| `bool ok = specification.IsSatisfiedBy(entity);`<br>`var errors = specification.Errors;` | `var errors = await specification.EvaluateAsync(entity);` |
| `StopOnFirstEntitySpecificationError = true;` en el constructor | `: base(stopOnFirstEntitySpecificationError: true)` |
| `protected override Task<List<SpecificationError>> ValidateSpecificationsAsync(T entity)` | ...`(T entity, CancellationToken cancellationToken = default)` |
| `services.AddDomainSpecificationsValidator();` vía `DependencyContainer` | Igual, pero la clase se llama `DependencyInjection` |
| `IEnumerable<SpecificationError> ValidationResult.Errors` | `IReadOnlyList<SpecificationError> ValidationResult.Errors` |

Lo que **no** cambia: cómo se declara una especificación. `Property(...)`, las reglas fluidas y
sus mensajes son idénticos, así que el cuerpo de tus constructores se queda como está.

Dos avisos:

- Si registrabas tus especificaciones como `Singleton`, en `1.0.x` estabas expuesto a
  veredictos cruzados bajo concurrencia. En `2.0.0` `Singleton` pasa a ser seguro, pero
  `Scoped` sigue siendo lo recomendable si la especificación depende de un repositorio.
- `SetValidator` ya no bloquea el hilo. Si tenías un `.Result` o un `.Wait()` alrededor de una
  validación para sortear el bloqueo anterior, quítalo.

La tabla de arriba recoge todas las rupturas. Si alguna te deja atascado, escríbeme por el
canal de soporte.

## Soporte

Para dudas, informes de error o peticiones de mejora abre una [incidencia](https://github.com/aldazsoft/persiltech.packages/issues).
También puedes consultar la [página del paquete](https://aldazsoft.github.io/DomainValidation/).

## Apoya el desarrollo

Si el paquete te ahorra trabajo, puedes apoyar su mantenimiento en
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).

---
packageName: Persiltech.Validation.Blazor
version: 0.1.0
---

# Propósito

Declarar la superficie pública de `Persiltech.Validation.Blazor` tal como está implementada.

# Superficie pública

## `Persiltech.Validation.Blazor`

```csharp
public sealed class ApiValidator : ComponentBase, IDisposable
{
    public IReadOnlyList<string> Unmatched { get; }

    [Parameter]
    public EventCallback<IReadOnlyList<string>> UnmatchedChanged { get; set; }

    public void Show(IReadOnlyDictionary<string, string[]>? errors);
    public void Clear();

    public void Dispose();
}
```

Eso es todo lo público. `FieldIdentifierResolver`, que traduce la clave de la API al campo del
modelo, es `internal`: es la pieza que hace el trabajo, pero exponerla ataría la firma del
paquete a cómo se recorren hoy las rutas.

# Contrato

## Qué recibe

Un diccionario de errores por campo, la forma exacta de `ValidationProblemDetails`. La clave
nombra el campo como lo serializó el servidor y puede traer ruta.

| Clave              | Destino                                                       |
| ------------------ | ------------------------------------------------------------- |
| `email`            | `model.Email` — exacta primero, luego ignorando mayúsculas     |
| `address.city`     | `City` del objeto `model.Address`, no del modelo raíz          |
| `items[0].name`    | `Name` del primer elemento, sea `List<T>` o `T[]`              |
| `byCode[abc].name` | `Name` del elemento con esa clave, por el indexador del tipo   |
| `""`               | `Unmatched`                                                    |
| desconocida        | `Unmatched`                                                    |

`Show(null)` equivale a una respuesta sin errores.

Los corchetes se resuelven por tres vías, en este orden: `Array` —cuyo indexador lo sirve el
entorno de ejecución y la reflexión no ve—, `IList`, y la propiedad `Item` del tipo para todo
lo demás.

## Qué hace

Escribe en el `ValidationMessageStore` del `EditContext` en cascada y avisa con
`NotifyValidationStateChanged`. No dibuja marcado propio.

## Cuándo retira lo escrito

| Momento                                    | Alcance                              |
| ------------------------------------------ | ------------------------------------ |
| `OnFieldChanged` de un campo con mensaje    | solo ese campo                       |
| `OnValidationRequested` (cada envío)        | todo, incluido `Unmatched`           |
| `Show(...)`                                 | todo, antes de poner lo nuevo        |
| `Clear()`                                   | todo                                 |
| Un `EditContext` nuevo en cascada           | todo, y se muda al contexto nuevo    |
| Desecharse                                  | todo, y se desengancha del contexto  |

El componente se engancha en `OnParametersSet`, no en `OnInitialized`, porque el contexto
puede cambiar bajo sus pies: `EditForm` monta uno nuevo cuando le cambian el modelo.

Editar un campo que no tenía mensaje **no** dispara `NotifyValidationStateChanged`: el evento
llega en cada pulsación de tecla y avisar siempre obligaría a revalidar el formulario entero
por letra escrita.

# Dependencias

`Microsoft.AspNetCore.Components.Web`, y nada más. Ninguna biblioteca de interfaz: el
componente trabaja contra el `EditContext`, así que sirve igual con MudBlazor y con las
entradas nativas de Blazor.

# Límites conocidos

- **`MudTextField` enseña un solo mensaje por campo**, el primero. Si la API devuelve varios
  bajo la misma clave, los demás no se ven. Es de MudBlazor, no del paquete.
- **El campo tiene que declarar `For`** (o `@bind-Value` en las entradas nativas). Sin eso no
  está atado a una propiedad y no hay adónde llevar el mensaje.
- **Un control sin `For` deja su mensaje invisible**, y no cae en `Unmatched`: la clave sí
  resolvió a una propiedad del modelo, solo que nadie la está mirando. Es el caso de
  `MudSelect` en selección múltiple, cuyo `For` apunta al valor único y no a la lista. Se
  resuelve con `<ValidationMessage For="..." />`, que lee del mismo `EditContext`, y
  notificando el cambio a mano cuando la selección se corrige.

# Comprobado sobre

`MudTextField` (una línea y `Lines`), `MudSelect` (única y múltiple), `MudAutocomplete`,
`MudNumericField`, `MudDatePicker`, `MudRadioGroup`, `MudCheckBox` y `MudSwitch`. La página
`/controles` del sample los tiene todos en un mismo formulario.

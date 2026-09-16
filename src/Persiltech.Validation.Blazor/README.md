# Persiltech.Validation.Blazor

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Validation.Blazor.svg)](https://www.nuget.org/packages/Persiltech.Validation.Blazor/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/aldazsoft/persiltech.packages/blob/main/LICENSE)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

Lleva los errores que devuelve una API al campo que los provocó. Los escribe en el
`EditContext` de Blazor, así que el componente de entrada los pinta **como si fueran suyos**:
el mismo texto rojo bajo el campo que saldría de una anotación de datos.

Sin él, un `400` con `ValidationProblemDetails` acaba en una lista de mensajes encima del
formulario, lejos del campo al que acusan y con una maquetación que cada aplicación repite a
su manera.

## Instalación

```
dotnet add package Persiltech.Validation.Blazor
```

## El contrato

```csharp
public sealed class ApiValidator : ComponentBase, IDisposable
{
    public void Show(IReadOnlyDictionary<string, string[]>? errors);
    public void Clear();

    public IReadOnlyList<string> Unmatched { get; }

    [Parameter]
    public EventCallback<IReadOnlyList<string>> UnmatchedChanged { get; set; }
}
```

Eso es todo. Un componente, dos métodos.

## Uso

Se coloca dentro del `EditForm`, igual que `DataAnnotationsValidator`, y se le pasan los
errores cuando la llamada vuelve:

```razor
<EditForm Model="model" OnValidSubmit="SubmitAsync">
    <DataAnnotationsValidator />
    <ApiValidator @ref="validator" />

    <MudTextField For="@(() => model.Email)" @bind-Value="model.Email" Label="Correo" />
    <MudTextField For="@(() => model.Password)" @bind-Value="model.Password" Label="Contraseña" />

    <MudButton ButtonType="ButtonType.Submit">Registrarme</MudButton>
</EditForm>

@code {
    private readonly RegistrationModel model = new();
    private ApiValidator validator = default!;

    private async Task SubmitAsync()
    {
        var result = await Api.RegisterAsync(model);

        validator.Show(result.Errors);
    }
}
```

**El campo tiene que declarar `For`.** Es lo que ata el componente de entrada a una propiedad
del modelo; sin eso no hay a qué campo llevar el mensaje. Con las entradas nativas de Blazor
(`InputText`) el papel lo hace `@bind-Value`.

Las dos validaciones conviven: `DataAnnotationsValidator` resuelve lo que el navegador puede
comprobar solo, y `ApiValidator` trae lo que solo sabe el servidor. Las dos escriben en el
mismo sitio.

## Lo que no es de ningún campo

`ValidationProblemDetails` manda los errores generales con la **clave vacía**, y una respuesta
puede nombrar un campo que este formulario no tiene. Descartarlos los dejaría invisibles, así
que se quedan en `Unmatched` para que los pintes donde decidas:

```razor
<ApiValidator @ref="validator" UnmatchedChanged="@(messages => unmatched = messages)" />

@if (unmatched.Count > 0)
{
    <MudAlert Severity="Severity.Error">
        @foreach (var message in unmatched)
        {
            <MudText Typo="Typo.body2">@message</MudText>
        }
    </MudAlert>
}
```

## Cómo encuentra el campo

La clave de la API nombra el campo como lo serializó el servidor, y Blazor lo identifica por el
objeto que lo contiene más el nombre de la propiedad. El componente recorre la ruta para dar
con ese objeto:

| Clave             | Va a parar a                                             |
| ----------------- | -------------------------------------------------------- |
| `email`           | `model.Email` — el emparejamiento ignora mayúsculas       |
| `address.city`    | `City` **de `model.Address`**, no del raíz                |
| `items[0].name`   | `Name` del primer elemento, sea lista o arreglo           |
| `byCode[abc].name`| `Name` del elemento con esa clave, si la colección indexa |
| `""` (vacía)      | `Unmatched`                                              |
| `telefono`        | `Unmatched`, si el modelo no tiene esa propiedad          |

Una ruta que no existe —una rama sin instanciar, un índice fuera de rango, una clave que no
está— no lanza: el mensaje se va a `Unmatched`. Un formulario no debe caerse por lo que
responda el servidor.

## Cuándo se retiran los mensajes

- **Al editar el campo**, porque el error acusaba al valor que se envió, no al que se está
  escribiendo.
- **Al volver a enviar**, porque lo que dijo la respuesta anterior deja de valer.
- **Al cambiar de modelo.** Si el formulario pasa a otro registro —el mismo diálogo que se
  reabre—, `EditForm` monta un `EditContext` nuevo y el componente se muda con él: lo del
  registro anterior no acompaña al siguiente.
- **Con `Clear()`**, cuando el formulario lo decida.

## Con MudBlazor

Funciona sin configurar nada. Está comprobado sobre `MudTextField` —de una línea y con
`Lines`—, `MudSelect`, `MudAutocomplete`, `MudNumericField`, `MudDatePicker`, `MudRadioGroup`,
`MudCheckBox` y `MudSwitch`: en todos el mensaje sale bajo el control con el mismo aspecto que
tendría el de una anotación. Tres cosas que conviene saber:

**`MudTextField` enseña un solo mensaje por campo**, el primero. Si tu API devuelve varios para
la misma clave, los demás no se ven. Cuando importen todos, júntalos en el servidor en un
mensaje o repártelos en claves distintas.

**Un control sin `For` no pinta nada, y su mensaje queda invisible** —no en `Unmatched`, porque
la clave sí encontró su propiedad—. Pasa, por ejemplo, con `MudSelect` en selección múltiple:
su `For` apunta al valor único, no a la lista. La salida es `ValidationMessage`, que es de
Blazor y lee del mismo sitio:

```razor
<MudSelect T="string" MultiSelection="true"
           SelectedValues="model.Interests"
           SelectedValuesChanged="OnInterestsChanged"
           Label="Qué te interesa">
    @* … *@
</MudSelect>

<MudText Typo="Typo.caption" Color="Color.Error">
    <ValidationMessage For="@(() => model.Interests)" />
</MudText>
```

**Y ese control tampoco avisa al corregirse**, así que el mensaje se quedaría puesto: el
formulario lo notifica a mano.

```csharp
private void OnInterestsChanged(IReadOnlyCollection<string> values)
{
    model.Interests = [.. values];

    editContext.NotifyFieldChanged(new FieldIdentifier(model, nameof(model.Interests)));
}
```

El paquete **no depende de MudBlazor** ni de ninguna biblioteca de interfaz: trabaja contra el
`EditContext`, así que sirve igual con las entradas nativas de Blazor.

## Decisiones de diseño

- **No habla HTTP.** Recibe los errores ya deserializados. Atarlo a un cliente concreto lo
  haría inservible para quien use otro, y el diccionario por campo es la misma forma en
  cualquiera de ellos.
- **No pinta nada.** Ni un cartel, ni un contenedor: escribe en el `EditContext` y deja que
  cada componente de entrada muestre lo suyo. Un componente propio para los errores nunca
  acabaría pareciéndose del todo al resto del formulario.
- **Lo que no encuentra dueño no se tira.** Es la diferencia entre un error que se ve en un
  sitio imperfecto y un error que no se ve.
- **Nada de dependencias de interfaz.** Obligaría a arrastrar MudBlazor a quien no la use.

## Compatibilidad

`net10.0`, sobre Blazor WebAssembly.

## Estado

Preliberación: la superficie pública puede cambiar antes de la 1.0.0.

## Historial de versiones

El código fuente vive en el [monorepo](https://github.com/aldazsoft/persiltech.packages); esta
tabla resume qué cambió en cada versión publicada.

| Versión | Cambios          |
| ------- | ---------------- |
| 0.1.0   | Primera versión. |

## Soporte

Para dudas, fallos o peticiones abre una [incidencia](https://github.com/aldazsoft/persiltech.packages/issues).
También puedes consultar la [página del paquete](https://aldazsoft.github.io/Validation.Blazor/).

## Apoyar el desarrollo

Si este paquete te resulta útil, puedes [patrocinar su desarrollo](https://github.com/sponsors/aldazsoft).

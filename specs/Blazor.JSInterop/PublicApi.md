---
packageName: Persiltech.Blazor.JSInterop
version: 1.1.1
---

# Propósito

Declarar la superficie pública de `Persiltech.Blazor.JSInterop` tal como está implementada.

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera este flujo, así
> que esta especificación no precedió al código: se levantó leyéndolo al homologar el
> paquete. Documenta lo que hay, no un diseño pendiente.

# Superficie pública

## `Persiltech.Blazor.JSInterop`

```csharp
public abstract class JSLoaderServiceBase : IAsyncDisposable
{
    protected JSLoaderServiceBase(ILogger logger, IJSRuntime jsRuntime, string jsModulePath);

    protected Task<T?> InvokeAsync<T>(string methodName, params object?[]? parameters);
    protected Task InvokeVoidAsync(string methodName, params object?[]? parameters);

    protected virtual ValueTask<IJSObjectReference> ResolveTargetAsync(IJSObjectReference jsModule);

    public virtual ValueTask DisposeAsync();
}

public abstract class WasmLoaderServiceBase : JSLoaderServiceBase
{
    protected WasmLoaderServiceBase(
        ILogger logger,
        IJSRuntime jsRuntime,
        string jsModulePath,
        string wasmModulePath,
        string wasmLoader = "wasmLoader");

    protected override ValueTask<IJSObjectReference> ResolveTargetAsync(IJSObjectReference jsModule);
}
```

El paquete publica además un recurso estático:
`_content/Persiltech.Blazor.JSInterop/wasmModuleLoader.js`, el cargador que
`WasmLoaderServiceBase` usa por omisión para instanciar el `.wasm`.

# Decisiones de diseño

- **La importación es perezosa**: el módulo se carga en la primera llamada, no al construir el
  servicio, y la misma referencia se reutiliza. Un servicio que nunca se usa no descarga nada.
- **Una importación fallida no se cachea.** La referencia sigue vacía y la siguiente llamada
  vuelve a intentarlo. Es lo que evita que el servicio quede muerto el resto del scope cuando
  el primer intento cae donde el navegador aún no es alcanzable —el prerender de Blazor Server,
  típicamente.
- **`DisposeAsync` no libera nada si no hubo llamadas**, precisamente por lo anterior, y tolera
  que el circuito ya no exista (`JSDisconnectedException`), de modo que liberar el servicio al
  cerrarse un circuito de Blazor Server nunca aflora como error.
- `WasmLoaderServiceBase` **libera el módulo de JavaScript en cuanto instancia el WebAssembly**:
  solo conserva la instancia, que es a la que van las llamadas. Lo libera también cuando la
  instanciación falla, y una instancia que no llega a materializarse produce una excepción
  explícita en vez de una referencia nula cacheada.
- `ResolveTargetAsync` es el punto de extensión que decide **a qué referencia van las llamadas**:
  por omisión, al propio módulo; `WasmLoaderServiceBase` lo redefine para apuntarlas a la
  instancia WebAssembly. Es lo que permite que las dos clases compartan toda la fontanería.
- Los errores de JSInterop **se registran y no se propagan**: `InvokeAsync` devuelve
  `default(T)` e `InvokeVoidAsync` retorna como si hubiera funcionado. Ver _Deuda conocida_.
- Cada fallo produce **una sola entrada de log**, con la excepción en su parámetro y el nombre
  del método invocado, generada con `[LoggerMessage]`.

# Fuera de alcance

- Registro en el contenedor de dependencias: cada servicio derivado decide su tiempo de vida.
- Cargar hojas de estilo. El consumidor las declara donde corresponde —un `<link>` en el
  componente, como hace `Persiltech.Leaflet.Blazor`—, no la clase base.
- Escribir el módulo de JavaScript o compilar el `.wasm`: eso corresponde al consumidor.

# Deuda conocida

- **Los errores se tragan.** Un fallo dentro del módulo deja `default(T)` sin que el llamador
  pueda distinguirlo de un resultado legítimo. Cambiarlo —propagar por omisión y ofrecer
  variantes tolerantes `TryInvokeAsync`— rompe el contrato y exige subir la versión mayor.
  Previsto para `2.0.0`.
- **No hay sobrecargas con `CancellationToken` ni con tiempo límite.** Una llamada que se cuelga
  —geolocalización esperando el permiso del usuario— cuelga al llamador indefinidamente.
  Previsto para `2.0.0`.
- **La ruta `_content/{ensamblado}/` la compone el consumidor.** Los paquetes Blazor de la
  casa repiten para ello un `ContentHelper` idéntico. Una sobrecarga de constructor que
  reciba la ruta relativa al `wwwroot` de la librería y resuelva sola el prefijo eliminaría esa
  duplicación. Previsto para `2.0.0`.

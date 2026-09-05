---
packageName: Persiltech.Localizer
version: 1.0.2
---

# Propósito

Declarar la superficie pública de `Persiltech.Localizer` tal como está implementada.

> **Nota sobre este archivo.** El paquete se escribió antes de que existiera este flujo, así
> que esta especificación no precedió al código: se levantó leyéndolo al homologar el
> paquete. Documenta lo que hay, no un diseño pendiente.

# Superficie pública

## `Persiltech.Localizer`

```csharp
public class LocalizationUtils<TEntity>
{
    public static string GetValue(string field);
    public static string GetValue(string field, CultureInfo cultureinfo);
}

public class CultureScope : IDisposable
{
    public CultureScope(CultureInfo culture);
    public void Dispose();
}
```

# Decisiones de diseño

- Los archivos de recursos se emparejan **por el nombre de `TEntity`**, con la convención
  `{Extractor}.{Culture}.resx`. El tipo genérico actúa de marcador: no se instancia.
- El `IStringLocalizer` se construye **una vez por tipo genérico cerrado** y se guarda en un
  campo estático, de modo que no se reconstruye en cada lectura.
- Una clave sin traducción **devuelve la propia clave**, que es cómo `IStringLocalizer` señala
  la ausencia. El paquete no lanza ni sustituye por un valor propio.
- `CultureScope` cambia `CurrentCulture` y `CurrentUICulture` del hilo y **restaura ambas** al
  liberarse, de modo que la sobrecarga de `GetValue` con cultura no deja el hilo alterado.

# Fuera de alcance

- Registro en el contenedor de dependencias: el acceso es estático y no hay nada que registrar.
- El middleware de localización de ASP.NET Core, que corresponde a la aplicación consumidora.
- Crear o escribir archivos de recursos: el paquete solo lee.

# Deuda conocida

- `CultureScope` no es `sealed` y no implementa el patrón completo de `IDisposable`
  (sin `Dispose(bool)` ni finalizador). No sostiene recursos no administrados, así que en la
  práctica no importa, pero cerrarlo exigiría subir la versión mayor.
- El parámetro `cultureinfo` de la segunda sobrecarga de `GetValue` no sigue el estilo
  `camelCase` habitual (`cultureInfo`). Renombrarlo cambia la superficie de los argumentos con
  nombre, así que queda anotado.

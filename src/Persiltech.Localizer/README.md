# Persiltech.Localizer

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Localizer.svg)](https://www.nuget.org/packages/Persiltech.Localizer/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

A simple tool for localizing .NET projects: strongly typed access to `.resx` resource files,
resolved from the current UI culture — or from a culture you pass in explicitly.

## Installation

    dotnet add package Persiltech.Localizer

## The contract

```csharp
namespace Persiltech.Localizer;

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

`TEntity` is a marker: it is never instantiated. Its **name** is what selects the resource
files, and the localizer built for it is cached in a static field, so it is not rebuilt on
every lookup.

A key with no translation **returns the key itself**. That is how `IStringLocalizer` reports a
missing entry, and this package neither throws nor substitutes a value of its own.

## Usage

First, create the resource files. Each file name must follow the format
`{Extractor}.{Culture}.resx`, where:

- **Extractor** — the name of the class used to read the file, for example `Messages`
- **Culture** — the culture identifier, for example `en-US` or `es-PE`

So a class named `Messages` reads from:

- `Messages.en-US.resx` (English, United States)
- `Messages.es-PE.resx` (Spanish, Peru)

In each file, create an entry with the **same key** and the value in the corresponding
language:

| Name | Value (`en-US`) | Value (`es-PE`) |
| --- | --- | --- |
| `Hello` | `Hello World!` | `¡Hola Mundo!` |

Then create the class that exposes each key:

```csharp
using Persiltech.Localizer;

public class Messages
{
    public static string Hello =>
        LocalizationUtils<Messages>.GetValue(nameof(Hello));
}
```

And use it. The value follows the UI culture of the thread:

```csharp
using System.Globalization;

Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
Console.WriteLine(Messages.Hello);   // Hello World!

Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-PE");
Console.WriteLine(Messages.Hello);   // ¡Hola Mundo!
```

### Reading one value in another culture

When you need a single value in a specific culture without disturbing the thread, pass the
culture in. It is applied through a `CultureScope`, which restores the previous culture
afterwards — even if the call throws:

```csharp
var greeting = LocalizationUtils<Messages>.GetValue(
    nameof(Messages.Hello), new CultureInfo("es-PE"));
```

`CultureScope` is public, so you can use it directly to run a whole block under one culture:

```csharp
using (new CultureScope(new CultureInfo("es-PE")))
{
    // Everything in here sees es-PE, on this thread.
}
```

### In an ASP.NET Core application

Configure the localization middleware so the UI culture is set per request:

```csharp
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en-US", "es-PE"];

    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});
```

Add it to the pipeline:

```csharp
app.UseRequestLocalization();
```

And read the values as usual:

```csharp
app.MapGet("/localizer/message", () => Results.Ok(Messages.Hello));
```

The value then follows the culture negotiated for each request, so the same endpoint answers
`Hello World!` to `Accept-Language: en-US` and `¡Hola Mundo!` to `Accept-Language: es-PE`.

## Design decisions

- Resource files are matched **by the name of `TEntity`**, following the
  `{Extractor}.{Culture}.resx` convention.
- The localizer is built **once per closed generic type** and cached statically.
- A missing key returns the key itself, rather than throwing or substituting a placeholder.
- `CultureScope` changes both `CurrentCulture` and `CurrentUICulture`, and restores both.

### Out of scope

- Dependency injection registration: access is static, so there is nothing to register.
- The ASP.NET Core localization middleware, which belongs to the consuming application.
- Creating or writing resource files. This package only reads.

## Compatibility

`net10.0`

## Version history

The source lives in the [monorepo](https://github.com/aldazsoft/persiltech.packages); this table summarises what each published version changed.

| Version | Changes |
| ------- | ------- |
| 1.0.2   | The project website now points to the portfolio page where the package is documented. The real licence text ships inside the `.nupkg` instead of an SPDX expression. The public surface is documented with XML comments, so IntelliSense works for consumers. No change to the public API. |
| 1.0.0 – 1.0.1 | Initial releases of `LocalizationUtils<TEntity>` and `CultureScope`. |

The public surface has not changed since `1.0.0`: everything published so far fixes packaging
and documentation, never the contract. Updating is always safe.

## Support

For questions, bug reports or feature requests open an [issue](https://github.com/aldazsoft/persiltech.packages/issues).
You can also see the [package page](https://aldazsoft.github.io/Localizer/).

## Support the development

If this package saves you work, you can support its maintenance on
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).

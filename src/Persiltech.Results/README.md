# Persiltech.Results

[![NuGet](https://img.shields.io/nuget/v/Persiltech.Results.svg)](https://www.nuget.org/packages/Persiltech.Results/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://aldazsoft.github.io/license/)
[![Sponsor](https://img.shields.io/badge/Sponsor-GitHub-ea4aaa.svg)](https://github.com/sponsors/aldazsoft)

An implementation of the Result pattern: an operation returns its success or its failure **as a
value**, with localized error messages, instead of throwing for outcomes you already expect.

## Installation

    dotnet add package Persiltech.Results

## The contract

There are **three shapes of result**, depending on what the operation has to return.

```csharp
namespace Persiltech.Results;

// No value. Only whether it worked, and why not.
public sealed class Result : ResultBase
{
    public static Result Success();
    public static Result Fail(params Error[] errors);
    public static Result Fail(string errorMessage);
}

// A value on the success side only.
public sealed class Result<TSuccess> : ResultBase
{
    public TSuccess Value { get; }

    public static Result<TSuccess> Success(TSuccess value);
    public static Result<TSuccess> Fail(params Error[] errors);
    public static Result<TSuccess> Fail(string errorMessage);
}

// A value of its own on each side. This is the railway one.
public sealed class Result<TSuccess, TError>
{
    public TSuccess Value { get; }   // throws when the result is a failure
    public TError Error { get; }     // throws when the result is a success
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
}
```

The first two share `ResultBase`, which carries the failures:

```csharp
public class ResultBase
{
    public Error[] Errors { get; }
    public Error? Error { get; }          // the first one, or null
    public string? ErrorMessage { get; }  // its message, or null
    public bool IsFailure { get; }
    public bool IsSuccess { get; }
}

public class Error(string? code, string message)
{
    public string? Code { get; }
    public string Message { get; }
}
```

`Code` is worth setting when the caller has to **decide** on the failure: it stays stable while
`Message` may be translated.

> **Results are never built with `new`.** They come from `Success` or `Fail`, so a result is
> always born in one of the two states and never half-way.

## Usage

### Returning a result

```csharp
using Persiltech.Results;

public Result<Customer> FindCustomer(int id)
{
    var customer = repository.Find(id);

    return customer is null
        ? Result<Customer>.Fail(new Error("customer.not-found", "No such customer."))
        : Result<Customer>.Success(customer);
}
```

### Reading it without tripping up

Use `Match`: it makes you handle both branches, so there is no way to read the wrong side.

```csharp
using Persiltech.Results.Extensions;

string message = FindCustomer(id).Match(
    onSuccess: customer => $"Found {customer.Name}.",
    onError: result => result.ErrorMessage ?? "Unknown failure.");
```

> **`Result<TSuccess>.Value` is not guarded.** On a failed result it returns the default value
> of `TSuccess` rather than throwing, so check `IsSuccess` first — or use `Match`, which is why
> these extensions exist.

### Chaining without checks in between

`Result<TSuccess, TError>` is the railway variant: each step runs only if the previous one
succeeded, and the first failure short-circuits the rest.

```csharp
Result<Invoice, Error> result = FindOrder(orderId)
    .Bind(order => ValidateStock(order))
    .Map(order => BuildInvoice(order))
    .OnSuccess(invoice => logger.LogInformation("Invoice {Id} issued", invoice.Id))
    .OnFailure(error => logger.LogWarning("Could not issue: {Message}", error.Message));
```

| Method | What it does |
| --- | --- |
| `Map(mapper)` | Transforms the success value. A failure passes through untouched |
| `MapError(mapper)` | Transforms the error. A success passes through untouched |
| `Bind(binder)` | Chains another operation that also returns a `Result` |
| `BindAsync(binder)` | The asynchronous version of `Bind` |
| `OnSuccess(action)` | Runs a side effect on success and returns the same result |
| `OnFailure(action)` | Runs a side effect on failure and returns the same result |
| `Match(onSuccess, onError)` | Collapses both branches into one value, or into an action |

Because it defines implicit conversions, you can return a value or an error directly:

```csharp
public Result<Customer, Error> FindCustomer(int id)
{
    var customer = repository.Find(id);

    if (customer is null)
    {
        return new Error("customer.not-found", "No such customer.");
    }

    return customer;
}
```

> With `TSuccess` and `TError` of the same type the conversion would be ambiguous. Use
> `Success` and `Fail` explicitly there.

## Design decisions

- Results are created only through `Success` and `Fail`, never with `new`.
- `Result<TSuccess, TError>` **throws when you read the wrong branch**, with a localized message
  from `Persiltech.Localizer`. Reading the value of a failed result is a programming mistake,
  not a case worth representing.
- `Map`, `Bind`, `OnSuccess` and `OnFailure` short-circuit, so a chain needs no intermediate
  checks from the caller.
- The extensions give the three shapes the same `Match`, so they are consumed alike.

### Out of scope

- Dependency injection registration: there is nothing to register.
- Converting exceptions into results, or the other way round. That is the consumer's call.
- Aggregating results of several operations into one.

## Compatibility

`net10.0`

## Version history

The source code is not public, so this is the package's change log.

| Version | Changes |
| ------- | ------- |
| 1.0.1   | The project website now points to the portfolio page where the package is documented. The real licence text ships inside the `.nupkg` instead of an SPDX expression. The public surface is documented with XML comments. This README is written from scratch: the previous one was three lines and named a package that does not exist. No change to the public API. |
| 1.0.0   | First release of `Result`, `Result<TSuccess>` and `Result<TSuccess, TError>`. |

This package **supersedes `Persiltech.Result`** (singular), which is no longer maintained.

## Support

The source code of this package is not public. For questions, bug reports or feature
requests, use the [package page](https://aldazsoft.github.io/Results/).

## Support the development

If this package saves you work, you can support its maintenance on
[GitHub Sponsors](https://github.com/sponsors/aldazsoft).

namespace Persiltech.Validation.Blazor.Sample.Models;

/// <summary>
/// Simula lo que devuelve una API de ASP.NET Core cuando rechaza una petición.
/// </summary>
/// <remarks>
/// Devuelve exactamente la forma de <c>ValidationProblemDetails</c>: las claves en camelCase,
/// tal como las serializa el servidor, y la clave vacía para lo que no es de ningún campo.
/// <para>
/// Es de mentira porque el paquete no habla HTTP. Recibe los errores ya deserializados, así que
/// montar un servidor solo añadiría ruido a lo que hay que ver.
/// </para>
/// </remarks>
public sealed class FakeApi
{
    /// <summary>
    /// Responde al registro con los errores que solo el servidor puede conocer.
    /// </summary>
    /// <param name="model">Lo que se envió.</param>
    /// <returns>Los errores por campo; vacío si lo aceptó.</returns>
    public async Task<IReadOnlyDictionary<string, string[]>> RegisterAsync(RegistrationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // La demora hace visible el estado de espera, como en una llamada de verdad.
        await Task.Delay(600);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        // Que un correo ya esté tomado no lo puede saber el navegador: hace falta preguntar.
        if (string.Equals(model.Email, "ocupado@example.com", StringComparison.OrdinalIgnoreCase))
        {
            errors["email"] = ["Ese correo ya tiene una cuenta."];
        }

        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
        {
            errors["password"] =
            [
                "La contraseña necesita al menos 8 caracteres.",
                "Y al menos un número."
            ];
        }

        if (string.Equals(model.Address.City, "Marte", StringComparison.OrdinalIgnoreCase))
        {
            errors["address.city"] = ["Todavía no repartimos allí."];
        }

        // Clave vacía: un error que no es de ningún campo. El componente lo deja en Unmatched
        // en lugar de tragárselo.
        if (string.Equals(model.FirstName, "error", StringComparison.OrdinalIgnoreCase))
        {
            errors[string.Empty] = ["El servicio de registro no está disponible ahora mismo."];
        }

        return errors;
    }

    /// <summary>
    /// Responde a la solicitud comprobando <b>todo</b> en el servidor.
    /// </summary>
    /// <param name="model">Lo que se envió.</param>
    /// <returns>Los errores por campo; vacío si la aceptó.</returns>
    /// <remarks>
    /// Que las comprobaciones obvias vivan aquí y no en una anotación es deliberado: enviar el
    /// formulario vacío devuelve un mensaje para cada control de golpe, que es lo que hay que
    /// mirar en esa página.
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, string[]>> SubmitRequestAsync(RequestModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        await Task.Delay(600);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(model.Comments))
        {
            errors["comments"] = ["Cuéntanos para qué lo necesitas."];
        }
        else if (model.Comments.Length < 20)
        {
            errors["comments"] = ["Con menos de 20 caracteres no podemos valorarlo."];
        }

        if (string.IsNullOrWhiteSpace(model.Country))
        {
            errors["country"] = ["Elige un país."];
        }
        else if (string.Equals(model.Country, "Marte", StringComparison.OrdinalIgnoreCase))
        {
            errors["country"] = ["Todavía no operamos allí."];
        }

        if (model.Interests.Count == 0)
        {
            errors["interests"] = ["Marca al menos un interés."];
        }
        else if (model.Interests.Count > 3)
        {
            errors["interests"] = ["Como mucho tres; el resto se añaden después."];
        }

        if (string.IsNullOrWhiteSpace(model.Plan))
        {
            errors["plan"] = ["Elige un plan."];
        }

        if (!model.AcceptsTerms)
        {
            errors["acceptsTerms"] = ["Hay que aceptar las condiciones para continuar."];
        }

        // Regla entre campos: el navegador no tiene por qué conocerla.
        if (model.Newsletter && string.IsNullOrWhiteSpace(model.Sector))
        {
            errors["newsletter"] = ["El boletín se arma por sector; declara uno antes."];
        }

        if (model.Employees is null)
        {
            errors["employees"] = ["Dinos cuántas personas sois."];
        }
        else if (model.Employees < 1)
        {
            errors["employees"] = ["Tiene que ser al menos una."];
        }

        if (model.StartDate is null)
        {
            errors["startDate"] = ["Elige una fecha de arranque."];
        }
        else if (model.StartDate < DateTime.Today)
        {
            errors["startDate"] = ["No puede ser anterior a hoy."];
        }

        if (string.IsNullOrWhiteSpace(model.Sector))
        {
            errors["sector"] = ["Elige un sector."];
        }
        else if (string.Equals(model.Sector, "Minería espacial", StringComparison.OrdinalIgnoreCase))
        {
            errors["sector"] = ["Ese sector todavía no lo cubrimos."];
        }

        return errors;
    }
}

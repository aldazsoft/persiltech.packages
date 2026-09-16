namespace Persiltech.Validation.Blazor.Tests;

/// <summary>
/// Modelo de las pruebas, con las formas que una clave de API puede nombrar.
/// </summary>
internal sealed class TestModel
{
    public string? Email { get; set; }

    public string? NewPassword { get; set; }

    public AddressModel Address { get; set; } = new();

    public AddressModel? Billing { get; set; }

    public List<LineModel> Items { get; set; } = [];

    /// <summary>Un arreglo, que no tiene propiedad <c>Item</c> que la reflexión encuentre.</summary>
    public LineModel[] Lines { get; set; } = [];

    /// <summary>Una colección que no es <c>IList</c> y solo se recorre por su indexador.</summary>
    public Dictionary<string, LineModel> ByCode { get; set; } = [];
}

internal sealed class AddressModel
{
    public string? City { get; set; }
}

internal sealed class LineModel
{
    public string? Name { get; set; }
}

namespace Persiltech.Validation.Blazor.Sample.Models;

/// <summary>
/// Listas de las que tiran los controles de selección.
/// </summary>
public static class Catalogs
{
    public static readonly string[] Countries =
        ["Perú", "Chile", "Colombia", "México", "España", "Marte"];

    public static readonly string[] Interests =
        ["Facturación", "Inventario", "Aduanas", "Transporte", "Analítica"];

    public static readonly string[] Plans =
        ["Básico", "Profesional", "Empresarial"];

    public static readonly string[] Sectors =
    [
        "Agroindustria",
        "Comercio exterior",
        "Logística",
        "Manufactura",
        "Minería espacial",
        "Servicios"
    ];
}

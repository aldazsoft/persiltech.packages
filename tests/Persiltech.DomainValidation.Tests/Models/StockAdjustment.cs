namespace Persiltech.DomainValidation.Tests.Models;

// Modelo con propiedades anulables por valor, que la restricción IComparable<T> de las reglas
// de comparación no admitía antes de la 2.0.0.
public class StockAdjustment
{
    public const string QuantityIsRequired = "Quantity is required.";

    public int? Quantity { get; set; }
    public int? ConfirmedQuantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

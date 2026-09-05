namespace Persiltech.DomainValidation.Tests.Models;

public class CreateOrderDetail
{
    public const string ProductIdMessage = "Required product.";
    public const string UnitPriceMessage = "Price required.";
    public const string QuantityMessage = "Quantity required.";

    public int ProductId { get; set; }
    public double UnitPrice { get; set; }
    public short Quantity { get; set; }
}


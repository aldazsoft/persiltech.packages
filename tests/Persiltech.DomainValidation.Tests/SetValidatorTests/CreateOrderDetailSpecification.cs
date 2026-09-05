namespace Persiltech.DomainValidation.Tests.SetValidatorTests;

internal class CreateOrderDetailSpecification : DomainSpecificationBase<CreateOrderDetail>
{
    public CreateOrderDetailSpecification()
    {
        Property(d => d.ProductId)
            .GreaterThan(0, CreateOrderDetail.ProductIdMessage);

        Property(d => d.Quantity)
            .GreaterThan((short)0, CreateOrderDetail.QuantityMessage);

        Property(d => d.UnitPrice)
            .GreaterThan(0, CreateOrderDetail.UnitPriceMessage);
    }
}


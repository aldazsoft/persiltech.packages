namespace Persiltech.DomainValidation.Tests.SetValidatorTests;

internal class CreateOrderSpecification : DomainSpecificationBase<CreateOrder>
{
    public CreateOrderSpecification(
        IDomainSpecificationsValidator<CreateOrderDetail> orderDetailsValidator)
    {
        Property(o => o.CustomerId)
            .IsRequired();

        Property(o => o.OrderDetails)
            .NotEmpty();

        Property(o => o.OrderDetails)
            .SetValidator(orderDetailsValidator);
    }
}


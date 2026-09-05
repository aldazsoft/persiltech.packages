namespace Persiltech.DomainValidation.Tests.SetValidatorTests;

internal class SetValidatorTestData
{
    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[]
        {
            new CreateOrder
            {
                CustomerId = "ALFKI",
                OrderDetails = [
                    new CreateOrderDetail
                    {
                        ProductId = 1,
                        Quantity = 2,
                        UnitPrice = 3}
                    ]},
            new ValidationResult([])
        };

        yield return new object[]
        {
            new CreateOrder
            {
                CustomerId = "" ,
                OrderDetails = [
                    new CreateOrderDetail
                    {
                        ProductId = 1,
                        Quantity = 2,
                        UnitPrice = 3
                    }
                    ]},
            new ValidationResult([
                new SpecificationError("CustomerId",
                    ErrorMessages.IsRequired)
                ])
        };


        yield return new object[]
        {
            new CreateOrder
            {
                CustomerId = "ALFKI",
                OrderDetails = null!
            },
            new ValidationResult([
                new SpecificationError("OrderDetails",
                    ErrorMessages.NotEmpty)
                ])
        };

        yield return new object[]
        {
            new CreateOrder
            {
                CustomerId = "ALFKI",
                OrderDetails = []
            },
            new ValidationResult([
                new SpecificationError("OrderDetails",
                    ErrorMessages.NotEmpty)
                ])
        };

        yield return new object[]
        {
            new CreateOrder
            {
                CustomerId = "ALFKI",
                OrderDetails = [
                    new CreateOrderDetail
                    {
                        ProductId = 1,
                        Quantity = 2,
                        UnitPrice = 3
                    },
                    new CreateOrderDetail
                    {
                        ProductId = 0,
                        Quantity = 2,
                        UnitPrice = 3
                    },
                    new CreateOrderDetail
                    {
                        ProductId = 1,
                        Quantity = 0,
                        UnitPrice = 3
                    },
                    new CreateOrderDetail
                    {
                        ProductId = 1,
                        Quantity = 2,
                        UnitPrice = 0
                    }
                    ]},
            new ValidationResult([
                new SpecificationError("OrderDetails[1].ProductId",
                    CreateOrderDetail.ProductIdMessage),
                new SpecificationError("OrderDetails[2].Quantity",
                    CreateOrderDetail.QuantityMessage),
                new SpecificationError("OrderDetails[3].UnitPrice",
                    CreateOrderDetail.UnitPriceMessage),
            ])
        };
    }
}


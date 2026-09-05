namespace Persiltech.DomainValidation.Tests.SpecificationTest;

public class SpecificationTest
{
    ISpecification<CreateOrder> Specification =
        new Specification<CreateOrder>(entity =>
        {
            List<SpecificationError>? errors = null;

            if (string.IsNullOrWhiteSpace(entity.CustomerId))
            {
                errors = new List<SpecificationError>
                {
                    new SpecificationError("CustomerId",
                        CreateOrder.CustomerIdIsRequired)
                };
            }

            return errors ?? [];
        });

    [Fact]
    public async Task EvaluateAsync_ShouldReturnErrors_WhenValidationFails()
    {
        // Arrange
        var entity = new CreateOrder { CustomerId = null! };

        // Act
        var errors = await Specification.EvaluateAsync(entity);

        // Assert
        Assert.Single(errors);
        Assert.Equal(CreateOrder.CustomerIdIsRequired,
            errors.First().ErrorMessage);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnNoErrors_WhenValidationPasses()
    {
        // Arrange
        var entity = new CreateOrder { CustomerId = "ALFKI" };

        // Act
        var errors = await Specification.EvaluateAsync(entity);

        // Assert
        Assert.Empty(errors);
    }
}


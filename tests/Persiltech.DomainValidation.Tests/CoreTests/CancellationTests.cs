namespace Persiltech.DomainValidation.Tests.CoreTests;

// La 2.0.0 lleva el CancellationToken por todo el recorrido: validador, especificación, regla
// y, dentro de SetValidator, el validador de cada elemento de la colección.
public class CancellationTests
{
    [Fact]
    public async Task EvaluateAsync_ShouldThrow_WhenTheTokenIsAlreadyCancelled()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            x => x.CustomerId);

        tree.IsRequired();

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await tree.Specifications[0].EvaluateAsync(
                new CreateOrder { CustomerId = null! }, cancellation.Token));
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenTheAsynchronousRuleObservesTheToken()
    {
        // Arrange
        var specification = new SlowUniqueEmailSpecification();

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await specification.ValidateAsync(
                new UserRegistration { Email = "name@hotmail.com" },
                cancellation.Token));
    }

    [Fact]
    public async Task ValidateAsync_ShouldReachTheElementValidator_WhenSetValidatorIsCancelled()
    {
        // Arrange
        var elementValidator = new DomainSpecificationsValidator<CreateOrderDetail>(
            [new SlowQuantitySpecification()]);

        var specification = new OrderWithSlowDetailsSpecification(elementValidator);

        var entity = new CreateOrder { OrderDetails = [new CreateOrderDetail()] };

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await specification.ValidateAsync(entity, cancellation.Token));
    }
}

internal class SlowUniqueEmailSpecification : DomainSpecificationBase<UserRegistration>
{
    public SlowUniqueEmailSpecification() =>
        Property(u => u.Email).MustAsync(async (entity, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);

            return true;
        }, "The email provided already exists.");
}

internal class SlowQuantitySpecification : DomainSpecificationBase<CreateOrderDetail>
{
    public SlowQuantitySpecification() =>
        Property(d => d.Quantity).MustAsync(async (entity, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);

            return true;
        }, CreateOrderDetail.QuantityMessage);
}

internal class OrderWithSlowDetailsSpecification : DomainSpecificationBase<CreateOrder>
{
    public OrderWithSlowDetailsSpecification(
        IDomainSpecificationsValidator<CreateOrderDetail> validator) =>
        Property(o => o.OrderDetails).SetValidator(validator);
}

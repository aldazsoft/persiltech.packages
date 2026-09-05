namespace Persiltech.DomainValidation.Tests.CoreTests;

// Hasta la 1.0.x la evaluación dejaba los errores en una propiedad de la especificación, así
// que una misma instancia validando en paralelo —lo que ocurre si el consumidor la registra
// como Singleton— devolvía veredictos de otra entidad. Con la evaluación sin estado, no.
public class ReentrancyTests
{
    const int Iterations = 500;

    [Fact]
    public async Task ValidateAsync_ShouldReturnTheErrorsOfEachEntity_WhenOneInstanceValidatesInParallel()
    {
        // Arrange
        var specification = new EmailIsRequiredSpecification();

        var valid = new UserRegistration { Email = "name@hotmail.com" };
        var invalid = new UserRegistration { Email = null! };

        // Act
        var results = await Task.WhenAll(
            Enumerable.Range(0, Iterations).Select(index => Task.Run(async () =>
            {
                bool shouldBeValid = index % 2 == 0;

                var errors = await specification.ValidateAsync(
                    shouldBeValid ? valid : invalid);

                return errors.Count == 0 == shouldBeValid;
            })));

        // Assert
        Assert.DoesNotContain(false, results);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnTheErrorsOfEachEntity_WhenOneRuleEvaluatesInParallel()
    {
        // Arrange
        var tree = new PropertySpecificationsTree<CreateOrder, string>(
            x => x.CustomerId);

        tree.IsRequired();

        var specification = tree.Specifications[0];

        // Act
        var results = await Task.WhenAll(
            Enumerable.Range(0, Iterations).Select(index => Task.Run(async () =>
            {
                bool shouldBeValid = index % 2 == 0;

                var errors = await specification.EvaluateAsync(
                    new CreateOrder { CustomerId = shouldBeValid ? "ALFKI" : null! });

                return errors.Count == 0 == shouldBeValid;
            })));

        // Assert
        Assert.DoesNotContain(false, results);
    }
}

internal class EmailIsRequiredSpecification : DomainSpecificationBase<UserRegistration>
{
    public EmailIsRequiredSpecification() =>
        Property(u => u.Email).IsRequired(UserRegistration.IsRequiredErrorMessage);
}

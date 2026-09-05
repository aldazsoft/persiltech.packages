namespace Persiltech.DomainValidation.Tests.DomainSpecificationsValidatorTests;

internal class ConditionalUniqueEmailSpecification :
    DomainSpecificationBase<UserRegistration>
{
    public const string DuplicateEmailMessage =
        "The email provided already exists.";
    public const string TestEmail = "user@northwind.com";
    public ConditionalUniqueEmailSpecification() : base(true) { }

    protected override async Task<List<SpecificationError>>
        ValidateSpecificationsAsync(
        UserRegistration entity, CancellationToken cancellationToken = default)
    {
        List<SpecificationError> errors = [];

        // Simular una operación asíncrona, por ejemplo,
        // una búsqueda en una base de datos
        await Task.Delay(1000, cancellationToken);
        bool duplicateEmail = TestEmail.Equals(entity.Email);

        if (duplicateEmail)
            errors.Add(new SpecificationError("Email", DuplicateEmailMessage));

        return errors;
    }
}


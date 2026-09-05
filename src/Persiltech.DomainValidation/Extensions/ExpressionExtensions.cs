namespace Persiltech.DomainValidation.Extensions;

internal static class ExpressionExtensions
{
    public static string? GetPropertyName<T, TProperty>(
        this Expression<Func<T, TProperty>> propertyExpression)
    {
        string? propertyName = null;

        var body = propertyExpression.Body;

        if (body is UnaryExpression unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        if (body is MemberExpression memberExpression)
        {
            propertyName = memberExpression.Member.Name;
        }

        return propertyName;
    }
}


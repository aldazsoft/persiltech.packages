namespace Persiltech.Membership.Tests;

public class PagingAndResponsesTests
{
    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-3, 20, 1, 20)]
    [InlineData(2, 0, 2, 20)]
    [InlineData(2, 500, 2, 100)]
    [InlineData(3, 50, 3, 50)]
    public void NormalizeBoundsBothParameters(
        int page,
        int pageSize,
        int expectedPage,
        int expectedPageSize)
    {
        var (normalizedPage, normalizedPageSize) = Paging.Normalize(page, pageSize);

        Assert.Equal(expectedPage, normalizedPage);
        Assert.Equal(expectedPageSize, normalizedPageSize);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(41, 20, 3)]
    public void TotalPagesRoundsUp(int totalCount, int pageSize, int expected)
    {
        var response = new PagedResponse<string>([], 1, pageSize, totalCount);

        Assert.Equal(expected, response.TotalPages);
    }

    [Fact]
    public void TotalPagesIsZeroWhenThePageSizeIsNotUsable()
    {
        var response = new PagedResponse<string>([], 1, 0, 10);

        Assert.Equal(0, response.TotalPages);
    }

    [Fact]
    public void ToErrorsGroupsTheIdentityMessagesUnderTheCamelCaseMember()
    {
        var result = IdentityResult.Failed(
            new IdentityError { Code = "DuplicateRoleName", Description = "El rol ya existe." },
            new IdentityError { Code = "InvalidRoleName", Description = "El nombre no es válido." });

        var errors = IdentityErrors.ToErrors(result, nameof(CreateRoleRequest.Name));

        Assert.Equal(["El rol ya existe.", "El nombre no es válido."], errors["name"]);
    }

    [Fact]
    public void ToErrorsGroupsTheIdentityMessagesUnderTheEmptyKeyWhenNoMemberIsGiven()
    {
        var result = IdentityResult.Failed(
            new IdentityError { Code = "UserLockoutNotEnabled", Description = "No se puede bloquear." });

        var errors = IdentityErrors.ToErrors(result);

        Assert.Equal(["No se puede bloquear."], errors[string.Empty]);
    }
}

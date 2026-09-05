namespace Persiltech.Results.Tests;

public class NonGenericResultTests
{
    [Fact]
    public void Ok_ShouldBeSuccess()
    {
        // Arrange
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fail_WithError_ShouldBeFailure()
    {
        // Arrange
        var error = new Error("E001", "Something went wrong");
        var result = Result.Fail(error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Equal("Something went wrong", result.ErrorMessage);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Fail_WithMessage_ShouldSetErrorMessage()
    {
        // Arrange
        var result = Result.Fail("Unexpected error");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Unexpected error", result.ErrorMessage);
        Assert.Single(result.Errors);
        Assert.Equal("Unexpected error", result.Error!.Message);
    }

    [Fact]
    public void Match_ShouldInvokeOnSuccess_WhenResultIsSuccess()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var output = result.Match(
            onSuccess: () => "Success",
            onError: r => $"Error: {r.ErrorMessage}"
        );

        // Assert
        Assert.Equal("Success", output);
    }

    [Fact]
    public void Match_ShouldInvokeOnError_WhenResultIsFailure()
    {
        // Arrange
        var result = Result.Fail("Failure occurred");

        // Act
        var output = result.Match(
            onSuccess: () => "Success",
            onError: r => $"Error: {r.ErrorMessage}"
        );

        // Assert
        Assert.Equal("Error: Failure occurred", output);
    }

    [Fact]
    public void Match_Action_ShouldInvokeOnSuccess_WhenResultIsSuccess()
    {
        // Arrange
        var result = Result.Success();
        string? captured = null;

        // Act
        result.Match(
            onSuccess: () => captured = "Éxito",
            onError: r => captured = $"Error: {r.ErrorMessage}"
        );

        // Assert
        Assert.Equal("Éxito", captured);
    }

    [Fact]
    public void Match_Action_ShouldInvokeOnError_WhenResultIsFailure()
    {
        // Arrange
        var result = Result.Fail("Algo salió mal");
        string? captured = null;

        // Act
        result.Match(
            onSuccess: () => captured = "Éxito",
            onError: r => captured = $"Error: {r.ErrorMessage}"
        );

        // Assert
        Assert.Equal("Error: Algo salió mal", captured);
    }
}

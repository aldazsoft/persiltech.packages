namespace Persiltech.Membership.Blazor.Tests;

/// <summary>
/// Verifica el envoltorio por el que pasa toda llamada del cliente.
/// </summary>
public sealed class ApiResultTests
{
    [Fact]
    public void UnResultadoCorrectoLlevaElCuerpoYNingunError()
    {
        var result = ApiResult<string>.Success("hola", HttpStatusCode.OK);

        Assert.True(result.Succeeded);
        Assert.Equal("hola", result.Value);
        Assert.Empty(result.Errors);
        Assert.Equal(string.Empty, result.ErrorSummary);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public void UnResultadoCorrectoSinCuerpoSigueSiendoCorrecto()
    {
        var result = ApiResult<string>.Success(null, HttpStatusCode.NoContent);

        Assert.True(result.Succeeded);
        Assert.Null(result.Value);
    }

    [Fact]
    public void UnResultadoFallidoConservaLosErroresPorCampo()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["email"] = ["El correo no es válido."],
            ["password"] = ["La contraseña es obligatoria."]
        };

        var result = ApiResult<string>.Failure(errors, HttpStatusCode.BadRequest);

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(["El correo no es válido."], result.Errors["email"]);
    }

    [Fact]
    public void ElResumenJuntaTodosLosMensajes()
    {
        var errors = new Dictionary<string, string[]>
        {
            [string.Empty] = ["Credenciales inválidas.", "Vuelve a intentarlo."]
        };

        var result = ApiResult<string>.Failure(errors, HttpStatusCode.BadRequest);

        Assert.Equal("Credenciales inválidas. Vuelve a intentarlo.", result.ErrorSummary);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    public void UnFalloSinMensajesNoSeConfundeConUnAcierto(HttpStatusCode statusCode, int expectedCode)
    {
        // Succeeded se calcula a partir de los errores, así que un fallo sin ninguno se
        // comportaría como correcto. De ahí el mensaje de reserva.
        var result = ApiResult<string>.Failure(null, statusCode);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Contains(expectedCode.ToString(), result.ErrorSummary);
    }

    [Fact]
    public void UnFalloConDiccionarioVacioTambienLlevaMensajeDeReserva()
    {
        var result = ApiResult<string>.Failure(new Dictionary<string, string[]>(), HttpStatusCode.BadRequest);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.ErrorSummary);
    }
}

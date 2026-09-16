namespace Persiltech.Validation.Blazor.Tests;

public sealed class FieldIdentifierResolverTests
{
    [Fact]
    public void Resuelve_una_propiedad_de_primer_nivel()
    {
        var model = new TestModel();

        var resolved = FieldIdentifierResolver.TryResolve(model, "email", out var field);

        Assert.True(resolved);
        Assert.Same(model, field.Model);
        Assert.Equal(nameof(TestModel.Email), field.FieldName);
    }

    /// <summary>
    /// El servidor serializa en camelCase y la propiedad es PascalCase.
    /// </summary>
    /// <remarks>
    /// Sin ignorar mayúsculas no encontraría ninguna, y todos los errores acabarían huérfanos.
    /// </remarks>
    [Theory]
    [InlineData("newPassword")]
    [InlineData("NewPassword")]
    [InlineData("NEWPASSWORD")]
    public void Empareja_sin_distinguir_mayusculas(string key)
    {
        var resolved = FieldIdentifierResolver.TryResolve(new TestModel(), key, out var field);

        Assert.True(resolved);
        Assert.Equal(nameof(TestModel.NewPassword), field.FieldName);
    }

    /// <summary>
    /// El campo pertenece al objeto que lo contiene, no al raíz.
    /// </summary>
    /// <remarks>
    /// Es lo que hace que el mensaje aterrice en la entrada atada a <c>model.Address.City</c>:
    /// Blazor identifica un campo por ese par, y confundir el contenedor lo dejaría sin dueño.
    /// </remarks>
    [Fact]
    public void Resuelve_una_ruta_anidada_contra_el_objeto_que_la_contiene()
    {
        var model = new TestModel();

        var resolved = FieldIdentifierResolver.TryResolve(model, "address.city", out var field);

        Assert.True(resolved);
        Assert.Same(model.Address, field.Model);
        Assert.Equal(nameof(AddressModel.City), field.FieldName);
    }

    [Fact]
    public void Resuelve_un_elemento_de_una_coleccion()
    {
        var model = new TestModel { Items = [new LineModel(), new LineModel()] };

        var resolved = FieldIdentifierResolver.TryResolve(model, "items[1].name", out var field);

        Assert.True(resolved);
        Assert.Same(model.Items[1], field.Model);
        Assert.Equal(nameof(LineModel.Name), field.FieldName);
    }

    /// <summary>
    /// Un arreglo no tiene propiedad <c>Item</c>.
    /// </summary>
    /// <remarks>
    /// El indexador de <c>T[]</c> lo sirve el entorno de ejecución, no un miembro que la
    /// reflexión encuentre, así que buscarlo por ahí dejaba sin campo a todo modelo que
    /// declarase sus colecciones como arreglos.
    /// </remarks>
    [Fact]
    public void Resuelve_un_elemento_de_un_arreglo()
    {
        var model = new TestModel { Lines = [new LineModel(), new LineModel()] };

        var resolved = FieldIdentifierResolver.TryResolve(model, "lines[1].name", out var field);

        Assert.True(resolved);
        Assert.Same(model.Lines[1], field.Model);
        Assert.Equal(nameof(LineModel.Name), field.FieldName);
    }

    /// <summary>
    /// Una colección que no es lista se recorre por su indexador.
    /// </summary>
    [Fact]
    public void Resuelve_un_elemento_por_una_clave_que_no_es_numero()
    {
        var line = new LineModel();
        var model = new TestModel { ByCode = { ["abc"] = line } };

        var resolved = FieldIdentifierResolver.TryResolve(model, "byCode[abc].name", out var field);

        Assert.True(resolved);
        Assert.Same(line, field.Model);
        Assert.Equal(nameof(LineModel.Name), field.FieldName);
    }

    [Fact]
    public void No_resuelve_una_propiedad_que_el_modelo_no_tiene()
    {
        Assert.False(FieldIdentifierResolver.TryResolve(new TestModel(), "telefono", out _));
    }

    /// <summary>
    /// Una rama sin instanciar no tiene dónde colgar el mensaje.
    /// </summary>
    [Fact]
    public void No_resuelve_cuando_un_tramo_intermedio_es_nulo()
    {
        Assert.False(FieldIdentifierResolver.TryResolve(new TestModel(), "billing.city", out _));
    }

    [Theory]
    [InlineData("items[5].name")]
    [InlineData("items[uno].name")]
    [InlineData("items[0.name")]
    [InlineData("lines[5].name")]
    [InlineData("lines[uno].name")]
    [InlineData("byCode[falta].name")]
    public void No_resuelve_un_indice_que_no_describe_al_modelo(string key)
    {
        var model = new TestModel
        {
            Items = [new LineModel()],
            Lines = [new LineModel()],
            ByCode = { ["abc"] = new LineModel() }
        };

        Assert.False(FieldIdentifierResolver.TryResolve(model, key, out _));
    }

    /// <summary>
    /// La clave vacía es la de los errores generales de <c>ValidationProblemDetails</c>.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void No_resuelve_una_clave_vacia(string key)
    {
        Assert.False(FieldIdentifierResolver.TryResolve(new TestModel(), key, out _));
    }

    [Fact]
    public void No_resuelve_sin_modelo()
    {
        Assert.False(FieldIdentifierResolver.TryResolve(null!, "email", out _));
    }
}

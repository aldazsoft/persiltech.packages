namespace Persiltech.Validation.Blazor.Tests;

public sealed class ApiValidatorTests : IDisposable
{
    private readonly TestRenderer Renderer = new(new EmptyServiceProvider());
    private readonly TestModel Model = new();
    private readonly EditContext Context;

    public ApiValidatorTests() => Context = new EditContext(Model);

    [Fact]
    public async Task Pone_el_mensaje_en_el_campo_que_nombra_la_clave()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["email"] = ["Ese correo ya tiene una cuenta."] });

        Assert.Equal(
            ["Ese correo ya tiene una cuenta."],
            Context.GetValidationMessages(FieldFor(nameof(TestModel.Email))));
    }

    [Fact]
    public async Task Pone_todos_los_mensajes_que_traiga_un_campo()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new()
        {
            ["newPassword"] = ["Necesita ocho caracteres.", "Y un número."]
        });

        Assert.Equal(
            ["Necesita ocho caracteres.", "Y un número."],
            Context.GetValidationMessages(FieldFor(nameof(TestModel.NewPassword))));
    }

    [Fact]
    public async Task Pone_el_mensaje_de_una_ruta_anidada_en_el_objeto_que_la_contiene()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["address.city"] = ["Todavía no repartimos allí."] });

        Assert.Equal(
            ["Todavía no repartimos allí."],
            Context.GetValidationMessages(
                new FieldIdentifier(Model.Address, nameof(AddressModel.City))));
    }

    /// <summary>
    /// Un error general llega con la clave vacía.
    /// </summary>
    /// <remarks>
    /// Descartarlo lo dejaría invisible, que es peor que enseñarlo en un sitio imperfecto.
    /// </remarks>
    [Fact]
    public async Task Deja_en_Unmatched_lo_que_no_es_de_ningun_campo()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new()
        {
            [string.Empty] = ["El servicio no está disponible."],
            ["telefono"] = ["No reconocemos ese número."]
        });

        Assert.Equal(
            ["El servicio no está disponible.", "No reconocemos ese número."],
            validator.Unmatched);

        Assert.Empty(Context.GetValidationMessages());
    }

    [Fact]
    public async Task Avisa_cuando_cambia_lo_que_hay_en_Unmatched()
    {
        IReadOnlyList<string>? notified = null;

        var validator = await Renderer.MountAsync(
            Context,
            EventCallback.Factory.Create<IReadOnlyList<string>>(
                this,
                messages => notified = messages));

        await ShowAsync(validator, new() { [string.Empty] = ["No está disponible."] });

        Assert.Equal(["No está disponible."], notified);
    }

    /// <summary>
    /// El error acusaba al valor que se envió, no al que se está escribiendo.
    /// </summary>
    [Fact]
    public async Task Retira_el_mensaje_del_campo_que_se_edita()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new()
        {
            ["email"] = ["Ese correo ya tiene una cuenta."],
            ["newPassword"] = ["Necesita ocho caracteres."]
        });

        await Renderer.Dispatcher.InvokeAsync(() =>
            Context.NotifyFieldChanged(FieldFor(nameof(TestModel.Email))));

        Assert.Empty(Context.GetValidationMessages(FieldFor(nameof(TestModel.Email))));

        Assert.Equal(
            ["Necesita ocho caracteres."],
            Context.GetValidationMessages(FieldFor(nameof(TestModel.NewPassword))));
    }

    /// <summary>
    /// Un control que no avisa por su cuenta puede hacerlo el formulario.
    /// </summary>
    /// <remarks>
    /// Es el caso de una propiedad de colección: el desplegable de selección múltiple de
    /// MudBlazor ata su <c>For</c> al valor único, no a la lista, así que nunca notifica por
    /// ella. El formulario llama a <see cref="EditContext.NotifyFieldChanged"/> a mano y el
    /// mensaje se retira igual, sin que el componente tenga que saber de qué control venía.
    /// </remarks>
    [Fact]
    public async Task Retira_el_mensaje_de_una_coleccion_avisada_a_mano()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["items"] = ["Marca al menos uno."] });

        var field = FieldFor(nameof(TestModel.Items));

        Assert.Equal(["Marca al menos uno."], Context.GetValidationMessages(field));

        Model.Items.Add(new LineModel());

        await Renderer.Dispatcher.InvokeAsync(() => Context.NotifyFieldChanged(field));

        Assert.Empty(Context.GetValidationMessages(field));
    }

    /// <summary>
    /// Vaciar el almacén no repinta nada por sí solo.
    /// </summary>
    /// <remarks>
    /// Sin el aviso, el componente de entrada seguiría enseñando un mensaje que ya no existe.
    /// </remarks>
    [Fact]
    public async Task Avisa_del_cambio_de_estado_al_retirar_el_mensaje()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["email"] = ["Ese correo ya tiene una cuenta."] });

        var notified = 0;

        Context.OnValidationStateChanged += (_, _) => notified++;

        await Renderer.Dispatcher.InvokeAsync(() =>
            Context.NotifyFieldChanged(FieldFor(nameof(TestModel.Email))));

        Assert.Equal(1, notified);
    }

    /// <summary>
    /// Editar un campo que nunca tuvo mensaje no obliga a revalidar.
    /// </summary>
    /// <remarks>
    /// Cada pulsación de tecla dispara el evento; avisar en todas cargaría el formulario con
    /// una revalidación completa por letra escrita.
    /// </remarks>
    [Fact]
    public async Task No_avisa_al_editar_un_campo_sin_mensaje()
    {
        await MountAsync();

        var notified = 0;

        Context.OnValidationStateChanged += (_, _) => notified++;

        await Renderer.Dispatcher.InvokeAsync(() =>
            Context.NotifyFieldChanged(FieldFor(nameof(TestModel.Email))));

        Assert.Equal(0, notified);
    }

    [Fact]
    public async Task Cada_envio_sustituye_lo_que_dijo_el_anterior()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new()
        {
            ["email"] = ["Ese correo ya tiene una cuenta."],
            [string.Empty] = ["El servicio no está disponible."]
        });

        await ShowAsync(validator, new() { ["newPassword"] = ["Necesita ocho caracteres."] });

        Assert.Empty(Context.GetValidationMessages(FieldFor(nameof(TestModel.Email))));
        Assert.Empty(validator.Unmatched);

        Assert.Equal(
            ["Necesita ocho caracteres."],
            Context.GetValidationMessages(FieldFor(nameof(TestModel.NewPassword))));
    }

    [Fact]
    public async Task Una_respuesta_sin_errores_deja_el_formulario_limpio()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["email"] = ["Ese correo ya tiene una cuenta."] });

        await ShowAsync(validator, []);

        Assert.Empty(Context.GetValidationMessages());
    }

    [Fact]
    public async Task Una_respuesta_nula_no_rompe_nada()
    {
        var validator = await MountAsync();

        await Renderer.Dispatcher.InvokeAsync(() => validator.Show(null));

        Assert.Empty(Context.GetValidationMessages());
        Assert.Empty(validator.Unmatched);
    }

    /// <summary>
    /// Al volver a validar, lo que dijo el servidor deja de valer.
    /// </summary>
    [Fact]
    public async Task Vuelve_a_empezar_cuando_se_valida_el_formulario()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new()
        {
            ["email"] = ["Ese correo ya tiene una cuenta."],
            [string.Empty] = ["El servicio no está disponible."]
        });

        await Renderer.Dispatcher.InvokeAsync(() => Context.Validate());

        Assert.Empty(Context.GetValidationMessages());
        Assert.Empty(validator.Unmatched);
    }

    [Fact]
    public async Task Clear_retira_todo_lo_que_se_habia_mostrado()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new()
        {
            ["email"] = ["Ese correo ya tiene una cuenta."],
            [string.Empty] = ["El servicio no está disponible."]
        });

        await Renderer.Dispatcher.InvokeAsync(validator.Clear);

        Assert.Empty(Context.GetValidationMessages());
        Assert.Empty(validator.Unmatched);
    }

    /// <summary>
    /// Un formulario que cambia de modelo se lleva el componente consigo.
    /// </summary>
    /// <remarks>
    /// <see cref="EditForm"/> monta un <see cref="EditContext"/> nuevo cuando le cambian el
    /// modelo —el mismo diálogo reabierto con otro registro—. Atado solo al primero, el
    /// componente escribiría en un almacén que ya no mira nadie.
    /// </remarks>
    [Fact]
    public async Task Sigue_al_contexto_cuando_el_formulario_cambia_de_modelo()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["email"] = ["Ese correo ya tiene una cuenta."] });

        var second = new TestModel();
        var secondContext = new EditContext(second);

        await Renderer.RebindAsync(secondContext);

        // Lo que decía del registro anterior no acompaña al nuevo.
        Assert.Empty(Context.GetValidationMessages());
        Assert.Empty(validator.Unmatched);

        await ShowAsync(validator, new() { ["email"] = ["Tampoco este está libre."] });

        Assert.Equal(
            ["Tampoco este está libre."],
            secondContext.GetValidationMessages(
                new FieldIdentifier(second, nameof(TestModel.Email))));

        Assert.Empty(Context.GetValidationMessages());
    }

    /// <summary>
    /// Y deja de escuchar al que abandona.
    /// </summary>
    /// <remarks>
    /// Si siguiera enganchado, validar el formulario viejo vaciaría el almacén del nuevo, y
    /// los mensajes del registro que sí está en pantalla desaparecerían sin motivo.
    /// </remarks>
    [Fact]
    public async Task Se_desengancha_del_contexto_anterior_al_cambiar_de_modelo()
    {
        var validator = await MountAsync();

        var second = new TestModel();
        var secondContext = new EditContext(second);

        await Renderer.RebindAsync(secondContext);

        await ShowAsync(validator, new() { ["email"] = ["Ese correo ya tiene una cuenta."] });

        await Renderer.Dispatcher.InvokeAsync(() => Context.Validate());

        Assert.Equal(
            ["Ese correo ya tiene una cuenta."],
            secondContext.GetValidationMessages(
                new FieldIdentifier(second, nameof(TestModel.Email))));
    }

    /// <summary>
    /// Un formulario que se abandona no debe seguir atado a su contexto.
    /// </summary>
    [Fact]
    public async Task Se_desengancha_del_contexto_al_desecharse()
    {
        var validator = await MountAsync();

        await ShowAsync(validator, new() { ["email"] = ["Ese correo ya tiene una cuenta."] });

        validator.Dispose();

        var notified = 0;

        Context.OnValidationStateChanged += (_, _) => notified++;

        await Renderer.Dispatcher.InvokeAsync(() =>
            Context.NotifyFieldChanged(FieldFor(nameof(TestModel.Email))));

        Assert.Equal(0, notified);
    }

    /// <inheritdoc />
    public void Dispose() => Renderer.Dispose();

    private Task<ApiValidator> MountAsync() => Renderer.MountAsync(Context);

    private Task ShowAsync(ApiValidator validator, Dictionary<string, string[]> errors) =>
        Renderer.Dispatcher.InvokeAsync(() => validator.Show(errors));

    private FieldIdentifier FieldFor(string propertyName) => new(Model, propertyName);
}

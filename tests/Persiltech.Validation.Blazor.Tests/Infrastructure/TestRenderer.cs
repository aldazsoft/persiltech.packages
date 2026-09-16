namespace Persiltech.Validation.Blazor.Tests.Infrastructure;

/// <summary>
/// Renderizador mínimo para montar un componente fuera del navegador.
/// </summary>
/// <remarks>
/// <see cref="ApiValidator"/> recibe su <see cref="EditContext"/> por cascada, y un parámetro
/// en cascada solo lo puede poner un renderizador: asignarlo a mano por reflexión probaría
/// otra cosa distinta de la que ocurre en producción.
/// <para>
/// No dibuja nada —<see cref="UpdateDisplayAsync"/> no hace nada—: aquí no se comprueba el
/// marcado, sino qué escribe el componente en el contexto.
/// </para>
/// </remarks>
internal sealed class TestRenderer(IServiceProvider services)
    : Renderer(services, NullLoggerFactory.Instance)
{
    private ValidatorHost Host = null!;

    /// <inheritdoc />
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>
    /// Monta un <see cref="ApiValidator"/> bajo el contexto indicado y lo devuelve.
    /// </summary>
    internal async Task<ApiValidator> MountAsync(
        EditContext editContext,
        EventCallback<IReadOnlyList<string>> unmatchedChanged = default)
    {
        Host = new ValidatorHost
        {
            Context = editContext,
            UnmatchedChanged = unmatchedChanged
        };

        var componentId = AssignRootComponentId(Host);

        await Dispatcher.InvokeAsync(() =>
            RenderRootComponentAsync(componentId, ParameterView.Empty));

        return Host.Mounted;
    }

    /// <summary>
    /// Cambia el contexto en cascada sin desmontar el componente.
    /// </summary>
    /// <remarks>
    /// Es lo que hace <see cref="EditForm"/> al cambiar de modelo: monta un
    /// <see cref="EditContext"/> nuevo y lo baja por el mismo árbol.
    /// </remarks>
    internal Task RebindAsync(EditContext editContext) =>
        Dispatcher.InvokeAsync(() => Host.Rebind(editContext));

    /// <inheritdoc />
    protected override void HandleException(Exception exception) =>
        ExceptionDispatchInfo.Capture(exception).Throw();

    /// <inheritdoc />
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
}

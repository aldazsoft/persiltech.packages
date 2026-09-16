namespace Persiltech.Validation.Blazor.Tests.Infrastructure;

/// <summary>
/// Componente raíz que pone un <see cref="ApiValidator"/> bajo el contexto que se le indique.
/// </summary>
/// <remarks>
/// El contexto se puede cambiar después de montado, porque eso es justo lo que hace
/// <see cref="EditForm"/> cuando el formulario pasa a otro modelo.
/// </remarks>
internal sealed class ValidatorHost : IComponent
{
    private RenderHandle Handle;

    /// <summary>Contexto que se pasa en cascada.</summary>
    internal EditContext Context { get; set; } = null!;

    /// <summary>Aviso que recibe el componente al montarse.</summary>
    internal EventCallback<IReadOnlyList<string>> UnmatchedChanged { get; set; }

    /// <summary>El componente montado.</summary>
    internal ApiValidator Mounted { get; private set; } = null!;

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle) => Handle = renderHandle;

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters)
    {
        Render();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cambia el contexto en cascada y vuelve a dibujar, sin desmontar el componente.
    /// </summary>
    internal void Rebind(EditContext context)
    {
        Context = context;

        Render();
    }

    private void Render() => Handle.Render(builder =>
    {
        builder.OpenComponent<CascadingValue<EditContext>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<EditContext>.Value), Context);
        builder.AddComponentParameter(
            2,
            nameof(CascadingValue<EditContext>.ChildContent),
            (RenderFragment)(child =>
            {
                child.OpenComponent<ApiValidator>(0);
                child.AddComponentParameter(
                    1,
                    nameof(ApiValidator.UnmatchedChanged),
                    UnmatchedChanged);
                child.AddComponentReferenceCapture(
                    2,
                    instance => Mounted = (ApiValidator)instance);
                child.CloseComponent();
            }));
        builder.CloseComponent();
    });
}

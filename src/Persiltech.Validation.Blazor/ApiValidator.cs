namespace Persiltech.Validation.Blazor;

/// <summary>
/// Muestra los errores de validación que devuelve una API en el campo que los provocó.
/// </summary>
/// <remarks>
/// Se coloca dentro de un <see cref="EditForm"/>, como
/// <c>DataAnnotationsValidator</c>, y se le pasan los errores cuando la llamada vuelve:
/// <code>
/// &lt;EditForm Model="model" OnValidSubmit="SubmitAsync"&gt;
///     &lt;ApiValidator @ref="validator" /&gt;
///     &lt;MudTextField For="@(() =&gt; model.Email)" @bind-Value="model.Email" /&gt;
/// &lt;/EditForm&gt;
/// </code>
/// Los escribe en el <see cref="ValidationMessageStore"/> del contexto, de modo que cada
/// componente de entrada los pinta <b>como si fueran suyos</b>: el mismo texto rojo bajo el
/// campo que saldría de una anotación de datos. No hay que maquetar ningún cartel aparte.
/// <para>
/// Un mensaje cuya clave no corresponde a ninguna propiedad del modelo no se pierde: se queda
/// en <see cref="Unmatched"/> para que el formulario lo muestre donde decida. Ahí caen los
/// errores generales, que en <c>ValidationProblemDetails</c> llegan con la clave vacía.
/// </para>
/// </remarks>
public sealed class ApiValidator : ComponentBase, IDisposable
{
    private readonly HashSet<FieldIdentifier> Marked = [];

    private ValidationMessageStore MessageStore = null!;
    private EditContext? Subscribed;

    [CascadingParameter]
    private EditContext CurrentEditContext { get; set; } = null!;

    /// <summary>
    /// Mensajes que no pertenecen a ningún campo del modelo.
    /// </summary>
    /// <remarks>
    /// Vacío mientras no haya ninguno. Un error general —o uno que nombra un campo que este
    /// formulario no tiene— acabaría invisible si se descartara, y eso es peor que enseñarlo
    /// en un sitio imperfecto.
    /// </remarks>
    public IReadOnlyList<string> Unmatched { get; private set; } = [];

    /// <summary>
    /// Se invoca cuando cambia lo que hay en <see cref="Unmatched"/>.
    /// </summary>
    /// <remarks>
    /// El formulario que los pinta vive fuera de este componente, así que necesita enterarse
    /// para volver a dibujarse.
    /// </remarks>
    [Parameter]
    public EventCallback<IReadOnlyList<string>> UnmatchedChanged { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// Se engancha aquí y no en <c>OnInitialized</c> porque el contexto puede cambiar bajo los
    /// pies: cuando el formulario pasa a otro modelo —el mismo diálogo que se reabre con otro
    /// registro—, <see cref="EditForm"/> monta un <see cref="EditContext"/> nuevo. Atado solo
    /// al primero, el componente seguiría escribiendo en un almacén que ya no mira nadie y los
    /// errores del segundo registro no aparecerían.
    /// </remarks>
    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ApiValidator)} necesita un {nameof(EditContext)} en cascada. " +
                $"Colócalo dentro de un {nameof(EditForm)}.");
        }

        if (ReferenceEquals(CurrentEditContext, Subscribed))
        {
            return;
        }

        Unsubscribe();

        MessageStore = new ValidationMessageStore(CurrentEditContext);
        Marked.Clear();

        SetUnmatched([]);

        // Al corregir un campo se retira su mensaje: el error venía de lo que se envió, y
        // dejarlo puesto mientras se reescribe acusa de algo que ya no es cierto.
        CurrentEditContext.OnFieldChanged += OnFieldChanged;
        CurrentEditContext.OnValidationRequested += OnValidationRequested;

        Subscribed = CurrentEditContext;
    }

    /// <summary>
    /// Muestra los errores que devolvió la API.
    /// </summary>
    /// <param name="errors">
    /// Errores por campo, tal como los entrega <c>ValidationProblemDetails</c>: la clave nombra
    /// el campo y puede traer ruta —<c>address.city</c>, <c>items[0].name</c>—. La clave vacía
    /// es para los que no son de ningún campo.
    /// </param>
    public void Show(IReadOnlyDictionary<string, string[]>? errors)
    {
        MessageStore.Clear();
        Marked.Clear();

        List<string> unmatched = [];

        foreach (var entry in errors ?? new Dictionary<string, string[]>())
        {
            var messages = entry.Value ?? [];

            if (FieldIdentifierResolver.TryResolve(CurrentEditContext.Model, entry.Key, out var field))
            {
                foreach (var message in messages)
                {
                    MessageStore.Add(field, message);
                }

                Marked.Add(field);

                continue;
            }

            unmatched.AddRange(messages);
        }

        SetUnmatched(unmatched);

        CurrentEditContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Retira todo lo que se había mostrado.
    /// </summary>
    public void Clear()
    {
        MessageStore.Clear();
        Marked.Clear();

        SetUnmatched([]);

        CurrentEditContext.NotifyValidationStateChanged();
    }

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();

    /// <summary>
    /// Se desengancha del contexto y retira de él lo que había escrito.
    /// </summary>
    /// <remarks>
    /// Lo segundo importa al cambiar de modelo: sin ello, los mensajes del registro anterior
    /// se quedarían en su contexto, y bastaría con volver a él para verlos otra vez.
    /// </remarks>
    private void Unsubscribe()
    {
        if (Subscribed is null)
        {
            return;
        }

        MessageStore.Clear();

        Subscribed.OnFieldChanged -= OnFieldChanged;
        Subscribed.OnValidationRequested -= OnValidationRequested;
        Subscribed = null;
    }

    /// <summary>
    /// Retira el mensaje del campo que se acaba de editar.
    /// </summary>
    /// <remarks>
    /// Hace falta avisar del cambio de estado: vaciar el almacén no repinta nada por sí solo,
    /// y el mensaje se quedaría en pantalla acusando a un valor que ya no es el que se envió.
    /// <para>
    /// Solo se avisa si el campo tenía algo puesto por aquí. Cada pulsación de tecla dispara
    /// este evento, y notificar en todas obligaría a revalidar el formulario entero sin motivo.
    /// </para>
    /// </remarks>
    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        if (!Marked.Remove(args.FieldIdentifier))
        {
            return;
        }

        MessageStore.Clear(args.FieldIdentifier);

        CurrentEditContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Limpia al volver a validar.
    /// </summary>
    /// <remarks>
    /// Cada envío pregunta de nuevo al servidor, así que lo que dijo el anterior deja de valer.
    /// Si no se limpiara aquí, un error ya corregido seguiría en pantalla hasta que la
    /// respuesta nueva lo sustituyera.
    /// </remarks>
    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        MessageStore.Clear();
        Marked.Clear();

        SetUnmatched([]);
    }

    private void SetUnmatched(IReadOnlyList<string> messages)
    {
        var changed = Unmatched.Count != messages.Count ||
                      !Unmatched.SequenceEqual(messages, StringComparer.Ordinal);

        Unmatched = messages;

        if (changed && UnmatchedChanged.HasDelegate)
        {
            UnmatchedChanged.InvokeAsync(messages);
        }
    }
}

namespace Persiltech.Membership.Email;

/// <summary>
/// Compone el asunto y los dos cuerpos de un aviso a partir de su plantilla.
/// </summary>
/// <remarks>
/// Es público para que el consumidor pueda sustituir la sustitución de marcadores por un
/// motor con condicionales o bucles sin tocar el adaptador. El registro usa
/// <c>TryAddSingleton</c>, así que una implementación propia registrada antes de
/// <c>AddMembershipEmail</c> gana.
/// </remarks>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Compone el aviso indicado con los valores dados.
    /// </summary>
    /// <param name="templateName">
    /// Nombre de la plantilla, sin extensión (Ej. <c>EmailConfirmation</c>).
    /// </param>
    /// <param name="values">
    /// Valores de los marcadores propios del aviso. Los de la marca los aporta la
    /// implementación desde <see cref="MembershipEmailOptions"/>, y estos ganan si hay
    /// coincidencia de nombre.
    /// </param>
    /// <returns>El asunto y los dos cuerpos, listos para enviar.</returns>
    /// <exception cref="InvalidOperationException">
    /// La plantilla no existe, o usa un marcador que no corresponde a ningún valor.
    /// </exception>
    RenderedEmail Render(string templateName, IReadOnlyDictionary<string, string?> values);
}

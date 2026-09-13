namespace Persiltech.Membership;

/// <summary>
/// Cabeceras que el paquete lee de la petición.
/// </summary>
public static class MembershipHeaders
{
    /// <summary>
    /// Identifica qué aplicación cliente hace la llamada.
    /// </summary>
    /// <remarks>
    /// La envía el frontal en todas sus peticiones. El paquete la usa para una sola cosa: saber
    /// a qué portal tiene que devolver los enlaces de los correos cuando hay más de uno
    /// —administrativo y de clientes, por ejemplo—, porque el aviso de contraseña olvidada se
    /// pide sin haber iniciado sesión y ahí no hay token del que deducirlo.
    /// <para>
    /// <b>No es una credencial.</b> Cualquiera puede escribirla, así que solo sirve para elegir
    /// entre direcciones que el consumidor ya configuró; un valor desconocido cae en la de por
    /// defecto. No se usa para autorizar nada.
    /// </para>
    /// </remarks>
    public const string ClientId = "clientId";
}

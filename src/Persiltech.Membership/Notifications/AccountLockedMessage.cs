namespace Persiltech.Membership.Notifications;

/// <summary>
/// Aviso de que una cuenta acaba de quedar bloqueada por intentos fallidos.
/// </summary>
/// <remarks>
/// Existe para resolver un dilema del inicio de sesión: la respuesta HTTP <b>no</b> distingue
/// «contraseña incorrecta» de «cuenta bloqueada», y no debe hacerlo, porque distinguirlo
/// confirmaría a quien prueba correos cuáles tienen cuenta. Pero la persona dueña de la cuenta
/// sí necesita saberlo, o se queda intentándolo sin entender por qué su contraseña buena falla.
/// <para>
/// El correo separa a los dos: quien controla el buzón se entera, y quien aporrea el endpoint
/// sigue sin enterarse de nada. De paso es la primera señal de que alguien está probando
/// contraseñas contra esa cuenta, que en la respuesta HTTP no se ve.
/// </para>
/// <para>
/// <b>No lleva testigo.</b> Un enlace de reinicio aquí convertiría el aviso en un arma:
/// bastaría con fallar la contraseña de alguien para que le llegara al buzón un testigo válido
/// que esa persona no ha pedido.
/// </para>
/// </remarks>
/// <param name="UserId">Identificador de la cuenta.</param>
/// <param name="Email">Correo al que va dirigido el aviso.</param>
/// <param name="FirstName">Nombre del usuario, para personalizar el saludo.</param>
/// <param name="LastName">Apellido del usuario, para personalizar el saludo.</param>
/// <param name="LockoutMinutes">
/// Minutos que queda bloqueada, redondeados hacia arriba. Se manda la duración y no el instante
/// en que termina para no tener que acertar con la zona horaria de quien lee.
/// </param>
public sealed record AccountLockedMessage(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    int LockoutMinutes)
{
    /// <summary>
    /// Aplicación cliente desde la que se intentó entrar, tal como llegó en la cabecera
    /// <see cref="MembershipHeaders.ClientId"/>.
    /// </summary>
    /// <remarks>
    /// Vacía significa "no se sabe", y quien redacte el correo usará su marca por defecto.
    /// </remarks>
    public string? ClientKey { get; init; }
}

namespace Persiltech.UserServices.Abstractions;

/// <summary>
/// Output Port que expone la identidad y el estado de autenticación del usuario actual.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Indica si el usuario actual está autenticado.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Nombre de usuario (login) del usuario actual, o <see langword="null"/> si no se dispone de dicho dato.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Nombre completo del usuario actual, o <see langword="null"/> si no se dispone de dicho dato.
    /// </summary>
    string? FullName { get; }
}

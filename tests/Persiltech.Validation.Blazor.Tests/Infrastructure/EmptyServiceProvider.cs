namespace Persiltech.Validation.Blazor.Tests.Infrastructure;

/// <summary>
/// Contenedor vacío para el renderizador de pruebas.
/// </summary>
/// <remarks>
/// <see cref="ApiValidator"/> no inyecta nada, así que montar un contenedor de verdad solo
/// añadiría una dependencia al proyecto de pruebas sin cambiar el resultado.
/// </remarks>
internal sealed class EmptyServiceProvider : IServiceProvider
{
    /// <inheritdoc />
    public object? GetService(Type serviceType) => null;
}

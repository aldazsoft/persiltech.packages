namespace Persiltech.UserServices.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddHttpContextUserService_DevuelveLaMismaColeccion()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddHttpContextUserService());
    }

    [Fact]
    public void AddHttpContextUserService_RegistraElAccesorDeHttpContext()
    {
        var services = new ServiceCollection().AddHttpContextUserService();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IHttpContextAccessor));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddHttpContextUserService_RegistraElAdaptadorComoSingleton()
    {
        var services = new ServiceCollection().AddHttpContextUserService();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IUserService));

        Assert.Equal(typeof(HttpContextUserService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddHttpContextUserService_LlamadoDosVeces_NoDuplicaLosRegistros()
    {
        var services = new ServiceCollection()
            .AddHttpContextUserService()
            .AddHttpContextUserService();

        Assert.Single(services, service => service.ServiceType == typeof(IUserService));
        Assert.Single(services, service => service.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddHttpContextUserService_ConAdaptadorYaRegistrado_LoConserva()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IUserService>());

        services.AddHttpContextUserService();

        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(IUserService));

        Assert.Null(descriptor.ImplementationType);
    }

    [Fact]
    public void AddHttpContextUserService_ResuelveElAdaptadorDesdeElContenedor()
    {
        using var provider = new ServiceCollection().AddHttpContextUserService().BuildServiceProvider();

        var service = provider.GetRequiredService<IUserService>();

        Assert.IsType<HttpContextUserService>(service);
        Assert.False(service.IsAuthenticated);
    }
}

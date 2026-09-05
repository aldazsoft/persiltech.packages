namespace Persiltech.UserServices.Tests;

public class HttpContextUserServiceTests
{
    private const string PreferredUserNameClaimType = "preferred_username";
    private const string FullNameClaimType = "name";

    [Fact]
    public void Constructor_ConAccesorNulo_LanzaArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new HttpContextUserService(null!));

        Assert.Equal("httpContextAccessor", exception.ParamName);
    }

    [Fact]
    public void IsAuthenticated_SinHttpContext_EsFalse()
    {
        var service = CreateService(httpContext: null);

        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ConIdentidadNoAutenticada_EsFalse()
    {
        var service = CreateService(CreateHttpContext(new ClaimsIdentity()));

        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ConIdentidadAutenticada_EsTrue()
    {
        var service = CreateService(CreateHttpContext(CreateAuthenticatedIdentity()));

        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void UserName_SinHttpContext_EsNull()
    {
        var service = CreateService(httpContext: null);

        Assert.Null(service.UserName);
    }

    [Fact]
    public void UserName_ConUsuarioNoAutenticado_NoLeeLasReclamaciones()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "juan.perez")]);
        var service = CreateService(CreateHttpContext(identity));

        Assert.Null(service.UserName);
    }

    [Fact]
    public void UserName_TomaElNombreDeLaIdentidadAntesQueLasDemas()
    {
        var identity = CreateAuthenticatedIdentity(
            new Claim(ClaimTypes.Name, "juan.perez"),
            new Claim(PreferredUserNameClaimType, "jperez"),
            new Claim(ClaimTypes.Upn, "juan.perez@example.com"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("juan.perez", service.UserName);
    }

    [Fact]
    public void UserName_SinNombreDeIdentidad_TomaPreferredUserName()
    {
        var identity = CreateAuthenticatedIdentity(
            new Claim(PreferredUserNameClaimType, "jperez"),
            new Claim(ClaimTypes.Upn, "juan.perez@example.com"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("jperez", service.UserName);
    }

    [Fact]
    public void UserName_SinPreferredUserName_TomaElUpn()
    {
        var identity = CreateAuthenticatedIdentity(new Claim(ClaimTypes.Upn, "juan.perez@example.com"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("juan.perez@example.com", service.UserName);
    }

    [Fact]
    public void UserName_ConValoresEnBlanco_LosDescarta()
    {
        var identity = CreateAuthenticatedIdentity(
            new Claim(ClaimTypes.Name, "   "),
            new Claim(PreferredUserNameClaimType, " "),
            new Claim(ClaimTypes.Upn, "juan.perez@example.com"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("juan.perez@example.com", service.UserName);
    }

    [Fact]
    public void UserName_SinNingunaReclamacionQueLoAporte_EsNull()
    {
        var identity = CreateAuthenticatedIdentity(new Claim(ClaimTypes.Email, "juan.perez@example.com"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Null(service.UserName);
    }

    [Fact]
    public void FullName_SinHttpContext_EsNull()
    {
        var service = CreateService(httpContext: null);

        Assert.Null(service.FullName);
    }

    [Fact]
    public void FullName_ConUsuarioNoAutenticado_NoLeeLasReclamaciones()
    {
        var identity = new ClaimsIdentity([new Claim(FullNameClaimType, "Juan Pérez")]);
        var service = CreateService(CreateHttpContext(identity));

        Assert.Null(service.FullName);
    }

    [Fact]
    public void FullName_TomaLaReclamacionNameAntesQueElNombreCompuesto()
    {
        var identity = CreateAuthenticatedIdentity(
            new Claim(FullNameClaimType, "Juan Pérez"),
            new Claim(ClaimTypes.GivenName, "Juan Carlos"),
            new Claim(ClaimTypes.Surname, "Pérez Gómez"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("Juan Pérez", service.FullName);
    }

    [Fact]
    public void FullName_SinReclamacionName_UneNombreYApellidos()
    {
        var identity = CreateAuthenticatedIdentity(
            new Claim(ClaimTypes.GivenName, "Juan"),
            new Claim(ClaimTypes.Surname, "Pérez"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("Juan Pérez", service.FullName);
    }

    [Fact]
    public void FullName_SoloConNombre_OmiteElApellidoAusente()
    {
        var identity = CreateAuthenticatedIdentity(new Claim(ClaimTypes.GivenName, "Juan"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("Juan", service.FullName);
    }

    [Fact]
    public void FullName_SoloConApellido_OmiteElNombreAusente()
    {
        var identity = CreateAuthenticatedIdentity(new Claim(ClaimTypes.Surname, "Pérez"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("Pérez", service.FullName);
    }

    [Fact]
    public void FullName_ConValoresEnBlanco_LosDescarta()
    {
        var identity = CreateAuthenticatedIdentity(
            new Claim(FullNameClaimType, "  "),
            new Claim(ClaimTypes.GivenName, "   "),
            new Claim(ClaimTypes.Surname, "Pérez"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Equal("Pérez", service.FullName);
    }

    [Fact]
    public void FullName_SinNingunaReclamacionQueLoAporte_EsNull()
    {
        var identity = CreateAuthenticatedIdentity(new Claim(ClaimTypes.Upn, "juan.perez@example.com"));
        var service = CreateService(CreateHttpContext(identity));

        Assert.Null(service.FullName);
    }

    [Fact]
    public void Propiedades_SeEvaluanEnCadaLectura_YNoSeCachean()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var service = new HttpContextUserService(httpContextAccessor);

        httpContextAccessor.HttpContext.Returns(CreateHttpContext(CreateAuthenticatedIdentity(
            new Claim(ClaimTypes.Name, "juan.perez"),
            new Claim(FullNameClaimType, "Juan Pérez"))));

        Assert.True(service.IsAuthenticated);
        Assert.Equal("juan.perez", service.UserName);
        Assert.Equal("Juan Pérez", service.FullName);

        httpContextAccessor.HttpContext.Returns(CreateHttpContext(CreateAuthenticatedIdentity(
            new Claim(ClaimTypes.Name, "ana.lopez"),
            new Claim(FullNameClaimType, "Ana López"))));

        Assert.True(service.IsAuthenticated);
        Assert.Equal("ana.lopez", service.UserName);
        Assert.Equal("Ana López", service.FullName);

        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserName);
        Assert.Null(service.FullName);
    }

    private static HttpContextUserService CreateService(HttpContext? httpContext)
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return new HttpContextUserService(httpContextAccessor);
    }

    private static HttpContext CreateHttpContext(ClaimsIdentity identity) =>
        new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

    private static ClaimsIdentity CreateAuthenticatedIdentity(params Claim[] claims) =>
        new(claims, authenticationType: "TestScheme");
}

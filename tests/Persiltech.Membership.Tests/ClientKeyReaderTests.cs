namespace Persiltech.Membership.Tests;

public class ClientKeyReaderTests
{
    [Fact]
    public void ReadsTheHeaderWhenItComesOnce()
    {
        Assert.Equal("MegadBlazorAdmin", Context("MegadBlazorAdmin").ReadClientKey());
    }

    [Fact]
    public void TrimsTheSurroundingSpaces()
    {
        Assert.Equal("MegadBlazorAdmin", Context("  MegadBlazorAdmin  ").ReadClientKey());
    }

    [Fact]
    public void ReturnsNullWhenTheHeaderIsMissing()
    {
        Assert.Null(new DefaultHttpContext().ReadClientKey());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsNullWhenTheHeaderIsBlank(string value)
    {
        Assert.Null(Context(value).ReadClientKey());
    }

    /// <summary>
    /// Una cabecera repetida no la envía un frontal legítimo, así que no se elige uno: se
    /// descarta y el aviso sale con la dirección de por defecto.
    /// </summary>
    [Fact]
    public void ReturnsNullWhenTheHeaderComesMoreThanOnce()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[MembershipHeaders.ClientId] =
            new StringValues(["MegadBlazorAdmin", "MegadBlazorCustomer"]);

        Assert.Null(context.ReadClientKey());
    }

    private static DefaultHttpContext Context(string value)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[MembershipHeaders.ClientId] = value;

        return context;
    }
}

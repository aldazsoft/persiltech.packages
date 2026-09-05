namespace Persiltech.Blazor.JSInterop.Sample.Services;

public record struct GeolocationLatLong(double Latitude, double Longitude);

public class GeolocationService(
    ILogger<GeolocationService> logger,
    IJSRuntime jsRuntime) : JSLoaderServiceBase(
        logger,
        jsRuntime,
        "./js/geolocationModule.js")
{
    public async ValueTask<GeolocationLatLong> GetPosition() =>
        await InvokeAsync<GeolocationLatLong>("getPosition");
}

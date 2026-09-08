namespace Persiltech.Membership.Blazor;

/// <summary>
/// Registro en el contenedor de los servicios del paquete.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Nombre del <see cref="HttpClient"/> que el paquete registra y usa.
    /// </summary>
    /// <remarks>
    /// Se expone para que el consumidor pueda resolverlo y hacer sus propias llamadas a la
    /// misma API con el token ya puesto.
    /// </remarks>
    public const string HttpClientName = "Persiltech.Membership";

    /// <summary>
    /// Registra el cliente de la API, el almacén de testigos y el estado de autenticación.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación consumidora.</param>
    /// <param name="configureOptions">
    /// Rellena las <see cref="MembershipApiOptions"/>: al menos la dirección base.
    /// </param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> o <paramref name="configureOptions"/> es
    /// <see langword="null"/>: sin la dirección base no hay API a la que llamar, y es
    /// preferible fallar aquí que en la primera petición.
    /// </exception>
    /// <remarks>
    /// No llama a <c>AddMudServices</c>: MudBlazor es del consumidor, que ya lo registra para
    /// su propia interfaz, y hacerlo dos veces duplicaría sus proveedores.
    /// <para>
    /// El manejador que firma se engancha solo a este cliente con nombre, no a los del
    /// consumidor: un manejador global mandaría el token a cualquier dominio al que la
    /// aplicación llamara.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddMembershipBlazor(
        this IServiceCollection services,
        Action<MembershipApiOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Las opciones se comprueban aquí, no con ValidateOnStart: en Blazor WebAssembly no
        // hay host que arranque servicios, así que esa validación nunca correría y una
        // dirección base vacía se descubriría en la primera petición. El registro es el
        // momento más temprano que existe en este modelo de alojamiento.
        var options = new MembershipApiOptions();
        configureOptions(options);

        if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out _))
        {
            throw new ArgumentException(
                $"'{nameof(MembershipApiOptions.BaseAddress)}' tiene que ser una URL absoluta. " +
                $"Se recibió: '{options.BaseAddress}'.",
                nameof(configureOptions));
        }

        services.Configure(configureOptions);

        services.TryAddScoped<IMembershipTokenStore, LocalStorageTokenStore>();
        services.AddScoped<MembershipBearerTokenHandler>();

        services.AddHttpClient(HttpClientName, (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<MembershipApiOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseAddress, UriKind.Absolute);
            })
            .AddHttpMessageHandler<MembershipBearerTokenHandler>();

        services.AddScoped<IMembershipApiClient>(provider => new MembershipApiClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName),
            provider.GetRequiredService<IOptions<MembershipApiOptions>>()));

        services.AddScoped<MembershipAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(
            provider => provider.GetRequiredService<MembershipAuthenticationStateProvider>());

        return services;
    }
}

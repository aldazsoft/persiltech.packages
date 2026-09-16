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

        var validation = new MembershipApiOptionsValidator().Validate(name: null, options);

        if (validation.Failed)
        {
            throw new ArgumentException(
                string.Join(" ", validation.Failures ?? []),
                nameof(configureOptions));
        }

        services.Configure(configureOptions);

        // Los rótulos por defecto, para que los formularios funcionen sin configurar nada.
        // TryAddSingleton y no Configure: así una llamada previa a AddMembershipFormTexts gana,
        // y quien no llame a ninguna se queda con los de la casa.
        services.TryAddSingleton<IOptions<MembershipFormTexts>>(
            Options.Create(new MembershipFormTexts()));

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

    /// <summary>
    /// Sustituye los rótulos de los formularios.
    /// </summary>
    /// <remarks>
    /// Se llama <b>antes</b> que <see cref="AddMembershipBlazor"/>, o da igual el orden si se
    /// llama a esta: el registro del paquete usa <c>TryAdd</c> y no pisa lo que ya haya.
    /// <para>
    /// El delegado recibe los valores por defecto ya puestos, así que basta con tocar los que
    /// cambien:
    /// <code>
    /// services.AddMembershipFormTexts(texts =>
    /// {
    ///     texts.Email = "Usuario";
    ///     texts.Password = "Clave";
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configureTexts">Ajustes sobre los rótulos por defecto.</param>
    /// <returns>La misma colección, para poder encadenar.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> o <paramref name="configureTexts"/> es
    /// <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddMembershipFormTexts(
        this IServiceCollection services,
        Action<MembershipFormTexts> configureTexts)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureTexts);

        var texts = new MembershipFormTexts();
        configureTexts(texts);

        services.AddSingleton<IOptions<MembershipFormTexts>>(Options.Create(texts));

        return services;
    }
}

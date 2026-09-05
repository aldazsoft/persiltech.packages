namespace Persiltech.Membership.OAuth;

/// <summary>
/// Alta de las aplicaciones cliente en el servidor de autorización.
/// </summary>
public static class MembershipOAuthClientRegistrar
{
    /// <summary>
    /// Registra las aplicaciones cliente indicadas, o actualiza las que ya existan.
    /// </summary>
    /// <param name="provider">Proveedor de servicios de la aplicación consumidora.</param>
    /// <param name="clients">Aplicaciones cliente a registrar.</param>
    /// <param name="cancellationToken">Testigo de cancelación de la operación.</param>
    /// <returns>La tarea que representa el alta.</returns>
    /// <remarks>
    /// Se invoca desde el arranque del consumidor, después de aplicar las migraciones. Es
    /// idempotente: vuelve a describir el cliente si ya estaba, de modo que ejecutarlo en
    /// cada arranque deja siempre el registro al día.
    /// <para>
    /// Un cliente sin secreto es <em>público</em> y solo puede usar el flujo Authorization
    /// Code con PKCE. Uno con secreto es confidencial y puede usar además credenciales de
    /// cliente.
    /// </para>
    /// </remarks>
    public static async Task RegisterMembershipOAuthClientsAsync(
        this IServiceProvider provider,
        IReadOnlyList<MembershipOAuthClient> clients,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(clients);

        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in clients)
        {
            var descriptor = Describe(client);
            var existing = await manager.FindByClientIdAsync(client.ClientId, cancellationToken);

            if (existing is null)
            {
                await manager.CreateAsync(descriptor, cancellationToken);
            }
            else
            {
                await manager.UpdateAsync(existing, descriptor, cancellationToken);
            }
        }
    }

    private static OpenIddictApplicationDescriptor Describe(MembershipOAuthClient client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            DisplayName = client.DisplayName,
            ClientType = client.ClientSecret is null ? ClientTypes.Public : ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit
        };

        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

        if (client.ClientSecret is not null)
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
        }

        foreach (var uri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var scope in client.Scopes)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        return descriptor;
    }
}

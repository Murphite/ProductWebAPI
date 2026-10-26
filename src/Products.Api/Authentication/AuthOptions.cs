namespace Products.Api.Authentication;

/// <summary>
/// Users for the built-in demo token issuer. In production this service would only validate tokens
/// issued by an external identity provider (Entra ID, Auth0, Keycloak) and this section would be removed.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public IReadOnlyList<DemoUser> Users { get; init; } = [];
}

public sealed class DemoUser
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; init; } = [];
}

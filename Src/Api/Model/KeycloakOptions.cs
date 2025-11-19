namespace Api.Model;

/// <summary>
/// Configuration options for Keycloak integration
/// </summary>
public class KeycloakOptions
{
    /// <summary>
    /// The Keycloak realm name
    /// </summary>
    public string Realm { get; set; } = "";

    /// <summary>
    /// The Keycloak authentication server URL
    /// </summary>
    public string AuthServerUrl { get; set; } = "";

    /// <summary>
    /// The Keycloak user client ID (for login)
    /// </summary>
    public string UserClientId { get; set; } = "";

    /// <summary>
    /// The Keycloak user client secret (for login)
    /// </summary>
    public string UserClientSecret { get; set; } = "";

    /// <summary>
    /// The Keycloak admin client ID
    /// </summary>
    public string AdminClientId { get; set; } = "";

    /// <summary>
    /// The Keycloak admin client secret
    /// </summary>
    public string AdminClientSecret { get; set; } = "";
}
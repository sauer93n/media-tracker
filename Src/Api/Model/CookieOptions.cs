namespace Api.Model;

/// <summary>
/// Cookie configuration options
/// </summary>
public class CookieOptions
{
    /// <summary>
    /// Name of the access token cookie
    /// </summary>
    public string AccessTokenCookieName { get; set; } = "AccessToken";

    /// <summary>
    /// Name of the refresh token cookie
    /// </summary>
    public string RefreshTokenCookieName { get; set; } = "RefreshToken";

    /// <summary>
    /// Cookie domain
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Cookie path
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Enable httpOnly flag (prevents JavaScript access)
    /// </summary>
    public bool HttpOnly { get; set; } = true;

    /// <summary>
    /// Enable secure flag (HTTPS only)
    /// </summary>
    public bool Secure { get; set; } = true;

    /// <summary>
    /// SameSite attribute (Strict, Lax, None)
    /// </summary>
    public string SameSite { get; set; } = "Lax";
}

namespace Application.DTO;

/// <summary>
/// Login request supporting both username and email authentication
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Username for login (optional if email is provided)
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Email for login (optional if username is provided)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Password for authentication
    /// </summary>
    public required string Password { get; init; }
};
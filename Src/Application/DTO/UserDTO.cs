using System.Text.Json.Serialization;

namespace Application.DTO;

public class UserDTO
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
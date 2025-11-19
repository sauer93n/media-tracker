namespace Application.DTO;

public record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
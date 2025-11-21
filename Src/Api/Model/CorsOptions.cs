namespace Api.Model;

/// <summary>
/// CORS configuration options
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// Allowed origins (comma-separated or array)
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Allow credentials (for cookies, authorization headers)
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"];

    /// <summary>
    /// Allowed headers
    /// </summary>
    public string[] AllowedHeaders { get; set; } = ["*"];

    /// <summary>
    /// Headers to expose to the client
    /// </summary>
    public string[] ExposedHeaders { get; set; } = ["Content-Disposition", "X-Total-Count"];
}

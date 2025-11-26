using System.Text.Json;
using Api.Model;
using Microsoft.Extensions.Options;
using CookieOptions = Api.Model.CookieOptions;

namespace Api.Middleware;

public class CookieTokenMiddleware(RequestDelegate next, IOptions<KeycloakOptions> keycloakOptions,
    IOptions<CookieOptions> cookieOptions,
    IHttpClientFactory httpClientFactory)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var accessTokenPresent = context.Request.Cookies.TryGetValue("AccessToken", out var accessToken);
        var refreshTokenPresent = context.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken);
        if (accessTokenPresent && refreshTokenPresent)
        {
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            context.Request.Headers["RefreshToken"] = refreshToken;
        }
        else if (accessTokenPresent && !refreshTokenPresent)
        {
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
        }
        else if (refreshTokenPresent)
        {
            var result = await RefreshAccessTokenAsync(context, refreshToken);
            if (result.IsSuccess)
            {
                context.Request.Headers["Authorization"] = $"Bearer {result.AccessToken}";
                context.Request.Headers["RefreshToken"] = result.RefreshToken;
            }
        }

        await next(context);
    }

    /// <summary>
    /// Refreshes the access token using the refresh token
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests</param>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>Token refresh result with new access token</returns>
    private async Task<TokenRefreshResult> RefreshAccessTokenAsync(
        HttpContext context,
        string refreshToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("middleware");
            var tokenUrl = $"{keycloakOptions.Value.AuthServerUrl}/realms/{keycloakOptions.Value.Realm}/protocol/openid-connect/token";

            var refreshData = new Dictionary<string, string>
            {
                ["client_id"] = keycloakOptions.Value.UserClientId,
                ["client_secret"] = keycloakOptions.Value.UserClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };

            var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(refreshData));

            var content = response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return TokenRefreshResult.Failure("Failed to refresh token");

            using var doc = JsonDocument.Parse(await content);
            var root = doc.RootElement;
            var newAccessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Access token not found in refresh response");
            var newRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            // Update cookies with new tokens
            var cookieOpts = cookieOptions.Value;
            var accessTokenCookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = cookieOpts.HttpOnly,
                Secure = cookieOpts.Secure,
                SameSite = Enum.Parse<SameSiteMode>(cookieOpts.SameSite),
                Expires = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
                Path = cookieOpts.Path
            };

            if (!string.IsNullOrEmpty(cookieOpts.Domain))
                accessTokenCookieOptions.Domain = cookieOpts.Domain;

            context.Response.Cookies.Append(cookieOpts.AccessTokenCookieName, newAccessToken, accessTokenCookieOptions);

            // Update refresh token if provided
            if (!string.IsNullOrEmpty(newRefreshToken))
            {
                var refreshTokenCookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = cookieOpts.HttpOnly,
                    Secure = cookieOpts.Secure,
                    SameSite = Enum.Parse<SameSiteMode>(cookieOpts.SameSite),
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    Path = cookieOpts.Path
                };

                if (!string.IsNullOrEmpty(cookieOpts.Domain))
                    refreshTokenCookieOptions.Domain = cookieOpts.Domain;

                context.Response.Cookies.Append(cookieOpts.RefreshTokenCookieName, newRefreshToken, refreshTokenCookieOptions);
            }

            return TokenRefreshResult.Success(newAccessToken, newRefreshToken);
        }
        catch (Exception ex)
        {
            return TokenRefreshResult.Failure($"Token refresh exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Result of token refresh operation
    /// </summary>
    internal class TokenRefreshResult
    {
        public bool IsSuccess { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ErrorMessage { get; set; }

        public static TokenRefreshResult Success(string accessToken, string refreshToken) =>
            new() { IsSuccess = true, AccessToken = accessToken, RefreshToken = refreshToken };

        public static TokenRefreshResult Failure(string errorMessage) =>
            new() { IsSuccess = false, ErrorMessage = errorMessage };
    }
}

/// <summary>
/// Extension method to add the CookieTokenMiddleware to the pipeline
/// </summary>
public static class CookieTokenMiddlewareExtensions
{
    public static IApplicationBuilder UseCookieTokenMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CookieTokenMiddleware>();
    }
}
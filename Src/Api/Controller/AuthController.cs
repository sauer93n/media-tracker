using System.Net.Http.Headers;
using System.Text.Json;
using Api.Model;
using Application.DTO;
using Application.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CookieOptions = Api.Model.CookieOptions;

namespace Api.Controller
{
    /// <summary>
    /// Authentication controller handling user registration, login, logout, and profile retrieval
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IOptions<KeycloakOptions> keycloakOptions;
        private readonly IOptions<CookieOptions> cookieOptions;
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the AuthController
        /// </summary>
        /// <param name="keycloakOptions">Keycloak configuration options</param>
        /// <param name="cookieOptions">Cookie configuration options</param>
        /// <param name="httpClientFactory">HTTP client factory for making requests</param>
        public AuthController(
            IOptions<KeycloakOptions> keycloakOptions,
            IOptions<CookieOptions> cookieOptions,
            IHttpClientFactory httpClientFactory)
        {
            this.keycloakOptions = keycloakOptions;
            this.cookieOptions = cookieOptions;
            this.httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Registers a new user with Keycloak and sends verification email
        /// </summary>
        /// <param name="request">Registration details including username, email, and password</param>
        /// <returns>Success message with instruction to verify email</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var httpClient = httpClientFactory.CreateClient();
            var policy = ResiliencePolicies.GetCombinedPolicy();
            var url = $"{keycloakOptions.Value.AuthServerUrl}/admin/realms/{keycloakOptions.Value.Realm}/users";

            var adminData = new Dictionary<string, string>
            {
                ["client_id"] = keycloakOptions.Value.AdminClientId,
                ["client_secret"] = keycloakOptions.Value.AdminClientSecret,
                ["grant_type"] = "client_credentials"
            };

            var adminResponse = await policy.ExecuteAsync(async () =>
                await httpClient.PostAsync(
                    $"{keycloakOptions.Value.AuthServerUrl}/realms/{keycloakOptions.Value.Realm}/protocol/openid-connect/token",
                    new FormUrlEncodedContent(adminData)
                )
            );

            var adminContent = await adminResponse.Content.ReadAsStringAsync();

            if (!adminResponse.IsSuccessStatusCode)
                return BadRequest(adminContent);

            using var adminDoc = JsonDocument.Parse(adminContent);
            var adminRoot = adminDoc.RootElement;
            var adminToken = adminRoot.GetProperty("access_token").GetString()!;

            var userPayload = new
            {
                username = request.Username,
                email = request.Email,
                enabled = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = request.Password,
                        temporary = false
                    }
                }
            };

            var userRequest = new HttpRequestMessage(HttpMethod.Post, url);
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            userRequest.Content = new StringContent(JsonSerializer.Serialize(userPayload), System.Text.Encoding.UTF8, "application/json");

            var userResponse = await policy.ExecuteAsync(async () =>
                await httpClient.SendAsync(userRequest)
            );
            var userContent = await userResponse.Content.ReadAsStringAsync();

            if (!userResponse.IsSuccessStatusCode)
                return BadRequest(userContent);

            // Extract user ID from response headers
            var userId = userResponse.Headers.Location?.AbsolutePath.Split('/').Last();

            if (string.IsNullOrEmpty(userId))
                return BadRequest("Failed to get user ID");

            return Ok(new { message = "User registered successfully. Please check your email to verify your account." });
        }

        /// <summary>
        /// Authenticates a user with username or email and password
        /// </summary>
        /// <param name="request">Login credentials (username or email with password)</param>
        /// <returns>Access token, refresh token, and expiration time</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Validate that either username or email is provided
            if (string.IsNullOrWhiteSpace(request.Username) && string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Either username or email must be provided");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required");

            var httpClient = httpClientFactory.CreateClient();
            var policy = ResiliencePolicies.GetCombinedPolicy();
            var tokenUrl = $"{keycloakOptions.Value.AuthServerUrl}/realms/{keycloakOptions.Value.Realm}/protocol/openid-connect/token";

            var loginData = new Dictionary<string, string>
            {
                ["client_id"] = keycloakOptions.Value.UserClientId,
                ["client_secret"] = keycloakOptions.Value.UserClientSecret,
                ["grant_type"] = "password",
                ["username"] = request.Username ?? request.Email ?? string.Empty,
                ["password"] = request.Password,
                ["scope"] = "openid profile email"
            };

            var response = await policy.ExecuteAsync(async () =>
                await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(loginData))
            );
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Unauthorized(content);

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Access token not found");
            var refreshToken = root.GetProperty("refresh_token").GetString() ?? throw new InvalidOperationException("Refresh token not found");
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            // Set httpOnly cookies for tokens
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

            var refreshTokenCookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = cookieOpts.HttpOnly,
                Secure = cookieOpts.Secure,
                SameSite = Enum.Parse<SameSiteMode>(cookieOpts.SameSite),
                Expires = DateTimeOffset.UtcNow.AddDays(7), // Refresh token typically lasts longer
                Path = cookieOpts.Path
            };

            if (!string.IsNullOrEmpty(cookieOpts.Domain))
                refreshTokenCookieOptions.Domain = cookieOpts.Domain;

            Response.Cookies.Append(cookieOpts.AccessTokenCookieName, accessToken, accessTokenCookieOptions);
            Response.Cookies.Append(cookieOpts.RefreshTokenCookieName, refreshToken, refreshTokenCookieOptions);

            // Return only non-sensitive data in response
            // Access token is now only in httpOnly cookie for better security
            var result = new
            {
                expiresIn,
                message = "Login successful. Tokens are stored in secure httpOnly cookies."
            };

            return Ok(result);
        }

        /// <summary>
        /// Logs out the user by invalidating their refresh token
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var httpClient = httpClientFactory.CreateClient();
            var policy = ResiliencePolicies.GetCombinedPolicy();
            var logoutUrl = $"{keycloakOptions.Value.AuthServerUrl}/realms/{keycloakOptions.Value.Realm}/protocol/openid-connect/logout";
            var cookieOpts = cookieOptions.Value;

            // Get the refresh token from cookie first, then fall back to Authorization header
            var refreshToken = Request.Cookies[cookieOpts.RefreshTokenCookieName];

            if (string.IsNullOrEmpty(refreshToken))
            {
                // Fall back to Authorization header for backward compatibility
                if (Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    var headerValue = authHeader.FirstOrDefault();
                    if (!string.IsNullOrEmpty(headerValue) && headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        refreshToken = headerValue.Substring("Bearer ".Length).Trim();
                    }
                }
            }

            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest("Refresh token is required for logout");

            var logoutData = new Dictionary<string, string>
            {
                ["client_id"] = keycloakOptions.Value.UserClientId,
                ["client_secret"] = keycloakOptions.Value.UserClientSecret,
                ["refresh_token"] = refreshToken
            };

            var response = await policy.ExecuteAsync(async () =>
                await httpClient.PostAsync(logoutUrl, new FormUrlEncodedContent(logoutData))
            );

            if (!response.IsSuccessStatusCode)
                return BadRequest("Failed to logout");

            // Clear cookies on logout
            Response.Cookies.Delete(cookieOpts.AccessTokenCookieName);
            Response.Cookies.Delete(cookieOpts.RefreshTokenCookieName);

            return Ok(new { message = "Successfully logged out" });
        }

        /// <summary>
        /// Retrieves the current user's profile information from Keycloak
        /// </summary>
        /// <returns>User profile with id, username, email, and other details</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var httpClient = httpClientFactory.CreateClient();
            var policy = ResiliencePolicies.GetCombinedPolicy();
            var cookieOpts = cookieOptions.Value;

            // Attempt to get user info with current access token
            var userInfoUrl = $"{keycloakOptions.Value.AuthServerUrl}/realms/{keycloakOptions.Value.Realm}/protocol/openid-connect/userinfo";
            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);

            string accessToken = null;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var headerValue = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(headerValue) && headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    accessToken = headerValue.Substring("Bearer ".Length).Trim();
                }
            }

            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken ?? string.Empty);

            var response = await policy.ExecuteAsync(async () =>
                await httpClient.SendAsync(userInfoRequest)
            );

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Unauthorized(content);

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var userInfo = new
            {
                id = root.GetProperty("sub").GetString(),
                username = root.GetProperty("preferred_username").GetString(),
                email = root.GetProperty("email").GetString(),
                emailVerified = root.TryGetProperty("email_verified", out var emailVerified) ? emailVerified.GetBoolean() : false,
                name = root.TryGetProperty("name", out var name) ? name.GetString() : null,
                givenName = root.TryGetProperty("given_name", out var givenName) ? givenName.GetString() : null,
                familyName = root.TryGetProperty("family_name", out var familyName) ? familyName.GetString() : null
            };

            return Ok(userInfo);
        }
    }
}
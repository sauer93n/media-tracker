using System.Text.Json;
using Api.Model;
using Application.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController: ControllerBase
    {
        private readonly IOptions<KeycloakOptions> options;
        private readonly HttpClient httpClient;

        public AuthController(IOptions<KeycloakOptions> options)
        {
            this.options = options;
            httpClient = new HttpClient();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var url = $"{options.Value.AuthServerUrl}/admin/realms/{options.Value.Realm}/users";

            var adminData = new Dictionary<string, string>
            {
                ["client_id"] = options.Value.AdminClientId,
                ["client_secret"] = options.Value.AdminClientSecret,
                ["grant_type"] = "client_credentials"
            };

            var adminResponse = await httpClient.PostAsync(
                $"{options.Value.AuthServerUrl}/realms/{options.Value.Realm}/protocol/openid-connect/token",
                new FormUrlEncodedContent(adminData)
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
            userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            userRequest.Content = new StringContent(JsonSerializer.Serialize(userPayload), System.Text.Encoding.UTF8, "application/json");

            var userResponse = await httpClient.SendAsync(userRequest);
            var userContent = await userResponse.Content.ReadAsStringAsync();

            if (!userResponse.IsSuccessStatusCode)
                return BadRequest(userContent);

            // Extract user ID from response headers
            var userId = userResponse.Headers.Location?.AbsolutePath.Split('/').Last();

            if (string.IsNullOrEmpty(userId))
                return BadRequest("Failed to get user ID");

            // Send verification email
            var verifyEmailUrl = $"{options.Value.AuthServerUrl}/admin/realms/{options.Value.Realm}/users/{userId}/send-verify-email";
            var verifyRequest = new HttpRequestMessage(HttpMethod.Put, verifyEmailUrl);
            verifyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var verifyResponse = await httpClient.SendAsync(verifyRequest);

            if (!verifyResponse.IsSuccessStatusCode)
                return BadRequest("Failed to send verification email");

            return Ok(new { message = "User registered successfully. Please check your email to verify your account." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var tokenUrl = $"{options.Value.AuthServerUrl}/realms/{options.Value.Realm}/protocol/openid-connect/token";

            var loginData = new Dictionary<string, string>
            {
                ["client_id"] = options.Value.UserClientId,
                ["client_secret"] = options.Value.UserClientSecret,
                ["grant_type"] = "password",
                ["username"] = request.Username,
                ["password"] = request.Password,
                ["scope"] = "openid profile email"
            };

            var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(loginData));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Unauthorized(content);

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Access token not found");
            var refreshToken = root.GetProperty("refresh_token").GetString() ?? throw new InvalidOperationException("Refresh token not found");
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            var result = new TokenResponse(accessToken, refreshToken, expiresIn);

            return Ok(result);
        }
    }
}
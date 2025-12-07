using System.Net.Http.Headers;
using System.Text.Json;
using Api.Model;
using Application.DTO;
using Application.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controller;

[Route("api/[controller]")]
[Authorize]
public class UsersController(
    ILogger<UsersController> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakOptions> keycloakOptions) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var httpClient = httpClientFactory.CreateClient();
        var policy = ResiliencePolicies.GetCombinedPolicy();
        var url = $"{keycloakOptions.Value.AuthServerUrl}/admin/realms/{keycloakOptions.Value.Realm}/users/{id}";

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

        var userRequest = new HttpRequestMessage(HttpMethod.Get, url);
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var userResponse = await policy.ExecuteAsync(async () =>
            await httpClient.SendAsync(userRequest)
        );
        var userContent = await userResponse.Content.ReadAsStringAsync();

        if (!userResponse.IsSuccessStatusCode)
            return BadRequest(userContent);

        return Ok(new { message = "User retrieved successfully.", user = JsonSerializer.Deserialize<UserDTO>(userContent) });
    }
}
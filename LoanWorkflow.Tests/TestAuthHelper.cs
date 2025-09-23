using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public static class TestAuthHelper
{
    public static async Task AuthenticateAsRoleAsync(HttpClient client, string role)
    {
        var user = LoanWorkflow.Shared.Workflow.MockUsers.Users.First(u => u.Role == role);
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        Assert.That(loginResp.IsSuccessStatusCode, Is.True, $"Login failed ({(int)loginResp.StatusCode}): {await loginResp.Content.ReadAsStringAsync()}");
        var payload = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        Assert.That(payload?.Data, Is.Not.Null, "Login payload data null");
        var tokenString = payload!.Data!.AccessToken;
        // Basic validation of token contains role claim
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);
        Assert.That(token.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role), Is.True, "Role claim missing in token");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);
    }

    public static Task AuthenticateAsFtAsync(HttpClient client) => AuthenticateAsRoleAsync(client, "FT");
}

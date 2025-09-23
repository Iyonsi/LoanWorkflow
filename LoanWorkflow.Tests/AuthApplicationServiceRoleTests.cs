using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Services.ApplicationServices;
using Moq;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class AuthApplicationServiceRoleTests
{
    [Test]
    public async Task LoginAsync_MockUser_ContainsRoleClaim()
    {
        // pick a known mock user (first role)
        var mockUser = LoanWorkflow.Shared.Workflow.MockUsers.Users.First();
        var aes = new Mock<IAesEncryptionService>();
        var external = new Mock<IExternalAuthApiClient>();
        // external should not be called for mock user
        external.Setup(e=>e.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(false);
        var jwtOptions = new JwtOptions{ Issuer="TestIssuer", Audience="TestAudience", SigningKey=new string('y',32), ExpiryMinutes=5 };
        var svc = new AuthApplicationService(aes.Object, external.Object, jwtOptions);

        var resp = await svc.LoginAsync(new LoginRequestDto(mockUser.Email, mockUser.Password), "trace-2");
    NUnit.Framework.Assert.That(resp.Data, Is.Not.Null);
        var token = resp.Data!.AccessToken;
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
    NUnit.Framework.Assert.That(roleClaim, Is.Not.Null);
    NUnit.Framework.Assert.That(roleClaim!.Value, Is.EqualTo(mockUser.Role));
    }
}

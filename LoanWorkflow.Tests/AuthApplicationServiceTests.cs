using System.Threading;
using System.Threading.Tasks;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Services.ApplicationServices;
using LoanWorkflow.Api.Features.Common;
using Moq;
using NUnit.Framework;

namespace LoanWorkflow.Tests;

public class AuthApplicationServiceTests
{
    [Test]
    public async Task LoginAsync_ReturnsToken_OnValidCredentials()
    {
        // Arrange
        var aes = new Mock<IAesEncryptionService>();
        var external = new Mock<IExternalAuthApiClient>();
        var jwtOptions = new JwtOptions{ Issuer = "TestIssuer", Audience = "TestAudience", SigningKey = new string('x', 32), ExpiryMinutes = 5 };
        aes.Setup(a=>a.Encrypt(It.IsAny<string>())).Returns("enc");
        external.Setup(e=>e.ValidateCredentialsAsync("user@test.com", "enc", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var svc = new AuthApplicationService(aes.Object, external.Object, jwtOptions);
        var dto = new LoginRequestDto("user@test.com", "password");
        // Act
        var resp = await svc.LoginAsync(dto, "trace-1");
        // Assert
    NUnit.Framework.Assert.That(resp.Code, Is.EqualTo("00")); // success code
    NUnit.Framework.Assert.That(resp.Data, Is.Not.Null);
    NUnit.Framework.Assert.That(resp.Data!.AccessToken, Is.Not.Empty);
    NUnit.Framework.Assert.That(resp.Data!.Email, Is.EqualTo("user@test.com"));
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Features.Common;
using LoanWorkflow.Api.Features.Constants;
using Microsoft.IdentityModel.Tokens;

namespace LoanWorkflow.Api.Services.ApplicationServices;

public interface IAuthApplicationService
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, string traceId, CancellationToken ct = default);
}

public sealed class AuthApplicationService : IAuthApplicationService
{
    private readonly IAesEncryptionService _aes;
    private readonly IExternalAuthApiClient _external;
    private readonly JwtOptions _jwtOptions;

    public AuthApplicationService(IAesEncryptionService aes, IExternalAuthApiClient external, JwtOptions jwtOptions)
    {
        _aes = aes; _external = external; _jwtOptions = jwtOptions;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto dto, string traceId, CancellationToken ct = default)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return ApiResponse<LoginResponseDto>.Validation("Email and password required", traceId: traceId);
            // Check mock users first
            string? role = null;
            var mockUser = LoanWorkflow.Shared.Workflow.MockUsers.FindByEmail(dto.Email);
            if(mockUser is not null)
            {
                if(!string.Equals(mockUser.Password, dto.Password))
                    return ApiResponse<LoginResponseDto>.Failure("Invalid credentials", traceId: traceId);
                role = mockUser.Role; // bypass external auth for mock user
            }
            else
            {
                var encrypted = _aes.Encrypt(dto.Password);
                var ok = await _external.ValidateCredentialsAsync(dto.Email, encrypted, ct);
                if(!ok) return ApiResponse<LoginResponseDto>.Failure("Invalid credentials", traceId: traceId);
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
            var claims = new List<Claim>{ new Claim(JwtRegisteredClaimNames.Sub, dto.Email), new Claim(JwtRegisteredClaimNames.Email, dto.Email) };
            if(!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds
            );
            var jwt = tokenHandler.WriteToken(token);
            var response = new LoginResponseDto(jwt, expires, dto.Email);
            return ApiResponse<LoginResponseDto>.Success(response, "Login successful", ResponseCodes.SUCCESS, traceId);
        }
        catch(Exception ex)
        {
            return ApiResponse<LoginResponseDto>.Failure(ex.Message, traceId: traceId);
        }
    }
}

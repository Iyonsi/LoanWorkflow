namespace LoanWorkflow.Api.Features.Auth;

public sealed record LoginRequestDto(string Email, string Password);
public sealed record LoginResponseDto(string AccessToken, DateTime ExpiresAt, string Email);

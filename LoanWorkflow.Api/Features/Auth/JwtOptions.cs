namespace LoanWorkflow.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty; // symmetric key
    public int ExpiryMinutes { get; set; } = 60; // default 1 hour
}

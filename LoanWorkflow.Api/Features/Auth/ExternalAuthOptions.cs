namespace LoanWorkflow.Api.Features.Auth;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";
    public string BaseUrl { get; set; } = string.Empty; // e.g. https://auth.example.com
    public string ValidatePath { get; set; } = "/api/validate"; // relative endpoint
    // AES settings (Base64-encoded)
    public string AesKey { get; set; } = string.Empty; // 32 bytes (256-bit) recommended
    public string AesIV { get; set; } = string.Empty;  // 16 bytes (128-bit block size)
}

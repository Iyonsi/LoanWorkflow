using System.Collections.ObjectModel;

namespace LoanWorkflow.Shared.Workflow;

public static class MockUsers
{
    public sealed record MockUser(string Email, string Role, string Password);

    // Distinct roles derived from FlowConfiguration
    public static readonly IReadOnlyList<string> Roles = FlowConfiguration.Flows
        .SelectMany(kv => kv.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
        .ToList();

    // Simple deterministic password pattern; DO NOT use in production
    private static readonly List<MockUser> _users = Roles
        .Select(r => new MockUser($"{r.ToLowerInvariant()}@example.com", r, "Password123!"))
        .ToList();

    public static IReadOnlyList<MockUser> Users => new ReadOnlyCollection<MockUser>(_users);

    public static MockUser? FindByEmail(string email) => _users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
}

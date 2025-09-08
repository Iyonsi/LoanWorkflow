using System;

namespace LoanWorkflow.Shared.Domain;

public sealed class LoanRequestLog : BaseEntity
{
    public string LoanRequestId { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty; // e.g. FT, HOP
    public string Action { get; set; } = string.Empty; // SUBMITTED, APPROVED, REJECTED, REROUTED
    public string ActorUserId { get; set; } = string.Empty;
    public string? Comments { get; set; }
}


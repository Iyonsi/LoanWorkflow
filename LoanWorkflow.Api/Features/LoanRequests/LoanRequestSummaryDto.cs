using LoanWorkflow.Shared.Domain;

namespace LoanWorkflow.Api.Features.LoanRequests;

public sealed class LoanRequestSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string LoanType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public int StageIndex { get; set; }
    public LoanRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

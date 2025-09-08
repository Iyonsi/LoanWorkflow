using System;

namespace LoanWorkflow.Shared.Domain;

public enum LoanRequestStatus
{
    Pending,
    InProgress,
    Approved,
    Rejected,
    Cancelled
}

public sealed class LoanRequest : BaseEntity
{
    public int FlowType { get; set; } // 1,2,3
    public decimal Amount { get; set; }
    public string BorrowerId { get; set; } = string.Empty;
    public LoanRequestStatus Status { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public int StageIndex { get; set; }
}


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
    // Replaced FlowType (int) with LoanType (string)
    public string LoanType { get; set; } = string.Empty; // e.g. standard, multi_stage, flex_review
    public decimal Amount { get; set; }
    public string BorrowerId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // Customer full name (max length enforced via EF config / validation)
    public bool IsEligible { get; set; } // Stored flag only
    public LoanRequestStatus Status { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public int StageIndex { get; set; }
}


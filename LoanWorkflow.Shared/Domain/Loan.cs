using System;

namespace LoanWorkflow.Shared.Domain;

public sealed class Loan : BaseEntity
{
    public string LoanRequestId { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, BOOKED, CLOSED
}


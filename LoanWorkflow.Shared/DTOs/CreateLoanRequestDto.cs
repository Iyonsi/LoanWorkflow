using System.ComponentModel.DataAnnotations;
namespace LoanWorkflow.Shared.DTOs;

public class CreateLoanRequestDto
{
    [Range(1,double.MaxValue)]
    public decimal Amount { get; set; }
    [Required, StringLength(36, MinimumLength=1)]
    public string BorrowerId { get; set; } = string.Empty;
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    // LoanType not supplied here for specialized endpoints; if generic endpoint retained later we can add.
}


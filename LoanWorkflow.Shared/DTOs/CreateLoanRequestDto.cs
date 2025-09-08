using System.ComponentModel.DataAnnotations;
namespace LoanWorkflow.Shared.DTOs;

public class CreateLoanRequestDto
{
    [Range(1,double.MaxValue)]
    public decimal Amount { get; set; }
    [Required, StringLength(36, MinimumLength=1)]
    public string BorrowerId { get; set; } = string.Empty;
    [Range(1,3)]
    public int FlowType { get; set; }
}


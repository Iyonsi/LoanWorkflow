using System.ComponentModel.DataAnnotations;
namespace LoanWorkflow.Shared.DTOs;

public class DecisionDto
{
    [Required, StringLength(36)]
    public string RequestId { get; set; } = string.Empty;
    [Required, StringLength(36)]
    public string ActorUserId { get; set; } = string.Empty;
    [Required]
    public string Stage { get; set; } = string.Empty;
    public bool Approved { get; set; }
    [StringLength(500)]
    public string? Comments { get; set; }
}


using System.Collections.Generic;

namespace LoanWorkflow.Shared.Workflow;

public static class FlowConfiguration
{
    // LoanType -> ordered stages
    public static readonly Dictionary<string, string[]> Flows = new(StringComparer.OrdinalIgnoreCase)
    {
        {"standard", new[]{"FT","HOP","ZONAL_HEAD","ED"}},
        {"multi_stage", new[]{"FT","HOP","BRANCH","ZONAL_HEAD","ED"}},
        {"flex_review", new[]{"FT","HOP","FIN_INT_CTRL","MD"}},
    };
}

using System.Collections.Generic;

namespace LoanWorkflow.Shared.Workflow;

public static class FlowConfiguration
{
    // FlowType -> ordered stages
    public static readonly Dictionary<int, string[]> Flows = new()
    {
        {1, new[]{"FT","HOP","ZONAL_HEAD","ED"}},
        {2, new[]{"FT","HOP","BRANCH","ZONAL_HEAD","ED"}},
        {3, new[]{"FT","HOP","FIN_INT_CTRL","MD"}},
    };
}

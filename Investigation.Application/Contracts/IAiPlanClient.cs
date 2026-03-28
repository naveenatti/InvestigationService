using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Models;

namespace Investigation.Application.Contracts
{
    public interface IAiPlanClient
    {
        /// <summary>
        /// POST /plan on the AI Agent.
        /// Sends structured tool definitions and available namespaces.
        /// Returns a structured execution plan.
        /// </summary>
        Task<PlanResponse> GetPlanAsync(
            string userQuery,
            List<ToolDefinitionDto> toolRegistry,
            List<string> namespaces,
            string traceId,
            CancellationToken ct = default);
    }
}

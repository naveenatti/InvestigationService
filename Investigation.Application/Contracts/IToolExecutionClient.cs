using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Investigation.Application.Models;

namespace Investigation.Application.Contracts
{
    public interface IToolExecutionClient
    {
        /// <summary>
        /// GET /api/engine/tools
        /// Fetches all registered, enabled tools with their parameter definitions.
        /// Called once before planning to build the tool registry for the AI Agent.
        /// </summary>
        Task<List<ToolDefinitionDto>> GetToolsAsync(CancellationToken ct = default);

        /// <summary>
        /// POST /api/engine/tools/list-namespaces/execute
        /// Fetches all available Kubernetes namespaces from the cluster.
        /// Called in parallel with GetToolsAsync before planning.
        /// </summary>
        Task<List<string>> GetNamespacesAsync(string traceId, CancellationToken ct = default);

        /// <summary>
        /// POST /api/engine/tools/{toolName}/execute
        /// Executes a single tool with the given arguments.
        /// Called for each step in the execution plan.
        /// </summary>
        Task<JsonObject> ExecuteToolAsync(
            string toolName,
            JsonObject? arguments,
            string traceId,
            CancellationToken ct = default);
    }
}
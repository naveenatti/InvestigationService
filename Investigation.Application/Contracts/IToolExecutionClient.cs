using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace Investigation.Application.Contracts
{
    public interface IToolExecutionClient
    {
        Task<JsonObject> ExecuteToolAsync(string toolName, JsonObject? arguments, string traceId, CancellationToken ct = default);
    }
}

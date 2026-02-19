using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace Investigation.Application.Contracts
{
    public interface IAgentClient
    {
        Task<JsonObject> CallAgentAsync(string query, string sessionId, string traceId, CancellationToken ct = default);
    }
}

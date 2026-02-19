using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace Investigation.Application.Contracts
{
    public interface IRagClient
    {
        Task<JsonObject> CallRagAsync(string queryHints, string traceId, CancellationToken ct = default);
    }
}

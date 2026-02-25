using System.Threading;
using System.Threading.Tasks;
using Investigation.Domain;

namespace Investigation.Application.Contracts
{
    public interface IAiAgentClient
    {
        Task<AgentResponse> InvestigateAsync(string query, string caseId, string traceId, CancellationToken ct = default);
    }
}

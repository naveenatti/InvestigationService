using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Models;

namespace Investigation.Application.Contracts
{
    public interface IAiPlanClient
    {
        Task<PlanResponse> GetPlanAsync(string userQuery, List<string> toolRegistry, string traceId, CancellationToken ct = default);
    }
}

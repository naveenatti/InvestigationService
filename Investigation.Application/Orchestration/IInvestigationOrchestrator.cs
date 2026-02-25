using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.DTOs;

namespace Investigation.Application.Orchestration
{
    /// <summary>
    /// Orchestrates investigation queries: validates requests, calls AI agent,
    /// maps results to tool calls, and returns typed responses.
    /// </summary>
    public interface IInvestigationOrchestrator
    {
        /// <summary>
        /// Executes an investigation for the given request.
        /// </summary>
        /// <param name="request">Investigation request with query, case, and user context.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Investigation response with results and tool calls.</returns>
        Task<InvestigationResponse> InvestigateAsync(InvestigationRequest request, CancellationToken ct = default);
    }
}



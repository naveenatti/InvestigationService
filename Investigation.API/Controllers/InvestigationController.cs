using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Investigation.Application.Contracts;
using Investigation.Application.DTOs;
using Investigation.Application.Orchestration;

namespace Investigation.API.Controllers
{
    /// <summary>
    /// Investigation API controller handling query endpoints for case analysis and troubleshooting.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvestigationController : ControllerBase
    {
        private readonly IInvestigationOrchestrator _orchestrator;
        private readonly ILogger<InvestigationController> _logger;
        private readonly ActivitySource _activitySource;

        public InvestigationController(
            IInvestigationOrchestrator orchestrator,
            ILogger<InvestigationController> logger,
            ActivitySource activitySource)
        {
            _orchestrator = orchestrator;
            _logger = logger;
            _activitySource = activitySource;
        }

        /// <summary>
        /// Executes an investigation query against a case.
        /// </summary>
        /// <remarks>
        /// Accepts an investigation request with query, case context, and user information.
        /// Orchestrates AI agent to analyze and provide insights.
        /// 
        /// Example request:
        /// 
        ///     POST /api/investigation/query
        ///     {
        ///       "traceId": "trace-123",
        ///       "caseId": "case-001",
        ///       "query": "Why is pod restarting?",
        ///       "context": {
        ///         "cluster": "prod"
        ///       },
        ///       "userId": "user-1"
        ///     }
        /// </remarks>
        /// <param name="request">Investigation query request containing query text, case ID, and context.</param>
        /// <returns>Investigation response with analysis results and executed tool calls.</returns>
        /// <response code="200">Investigation completed successfully with results.</response>
        /// <response code="400">Invalid request (missing required fields or validation error).</response>
        /// <response code="500">Internal server error during investigation processing.</response>
        [HttpPost("query")]
        [ProducesResponseType(typeof(InvestigationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InvestigationResponse>> Query([FromBody] InvestigationRequest request)
        {
            // Validate request
            if (request == null)
                return BadRequest("Request body cannot be null");

            // Ensure TraceId is set (extract from header if not provided)
            var traceId = string.IsNullOrWhiteSpace(request.TraceId)
                ? (Request.Headers.ContainsKey("X-Trace-Id")
                    ? Request.Headers["X-Trace-Id"].ToString()
                    : Activity.Current?.TraceId.ToString())
                ?? Guid.NewGuid().ToString()
                : request.TraceId;

            // Start OpenTelemetry activity for this request
            using var activity = _activitySource.StartActivity("investigation.request", ActivityKind.Server);
            if (activity is not null)
            {
                activity.SetIdFormat(ActivityIdFormat.W3C);
                activity.AddTag("traceId", traceId);
                activity.AddTag("caseId", request.CaseId);
                activity.AddTag("userId", request.UserId);
            }

            try
            {
                _logger.LogInformation(
                    "Investigation query received: TraceId={TraceId}, CaseId={CaseId}, UserId={UserId}",
                    traceId,
                    request.CaseId,
                    request.UserId);

                // Call orchestrator with validated request
                var response = await _orchestrator.InvestigateAsync(
                    request with { TraceId = traceId },
                    HttpContext.RequestAborted);

                _logger.LogInformation(
                    "Investigation query completed: TraceId={TraceId}, Status={Status}, DurationMs={DurationMs}",
                    traceId,
                    response.Status,
                    response.DurationMs);

                activity?.Stop();

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "Investigation query validation failed: TraceId={TraceId}, Message={Message}",
                    traceId,
                    ex.Message);

                activity?.AddTag("error", true);
                activity?.AddTag("error.message", ex.Message);

                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Investigation query failed: TraceId={TraceId}, CaseId={CaseId}",
                    traceId,
                    request.CaseId);

                activity?.AddTag("error", true);
                activity?.AddTag("error.message", ex.Message);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Investigation processing failed", traceId = traceId });
            }
        }
    }
}


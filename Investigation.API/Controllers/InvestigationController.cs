using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Investigation.Application.Commands;

namespace Investigation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvestigationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InvestigationController> _logger;

        public InvestigationController(IMediator mediator, ILogger<InvestigationController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] JsonObject payload)
        {
            // Extract trace id or create one
            var traceId = Request.Headers.ContainsKey("X-Trace-Id") ? Request.Headers["X-Trace-Id"].ToString() : Activity.Current?.Id ?? Activity.NewId().ToString();

            using var activity = new Activity("InvestigationController.Query");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            activity.AddTag("traceId", traceId);

            var query = payload["query"]?.GetValue<string>() ?? string.Empty;
            var sessionId = payload["sessionId"]?.GetValue<string>();

            var result = await _mediator.Send(new RunInvestigationCommand(query, sessionId, traceId));

            activity.Stop();

            return Ok(result);
        }
    }
}

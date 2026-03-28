using System.Threading.Tasks;
using Xunit;
using Moq;
using Investigation.Application.Orchestration;
using Investigation.Application.Contracts;
using Investigation.Application.Models;
using Investigation.Domain;
using Investigation.Application.DTOs;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Investigation.Tests
{
    public class InvestigationOrchestratorTests
    {
        private readonly Mock<IAiPlanClient> _mockPlanClient;
        private readonly Mock<IAiAgentClient> _mockAnalysisClient;
        private readonly Mock<IToolExecutionClient> _mockToolClient;
        private readonly ILogger<InvestigationOrchestrator> _logger;
        private readonly InvestigationOrchestrator _orchestrator;

        public InvestigationOrchestratorTests()
        {
            _mockPlanClient = new Mock<IAiPlanClient>();
            _mockAnalysisClient = new Mock<IAiAgentClient>();
            _mockToolClient = new Mock<IToolExecutionClient>();
            var mockSessionRepo = new Mock<ISessionRepository>();
            _logger = new Mock<ILogger<InvestigationOrchestrator>>().Object;

            var validatorLogger = new Mock<ILogger<PlanValidator>>().Object;
            var validator = new PlanValidator(validatorLogger);

            mockSessionRepo
                .Setup(r => r.SaveAsync(It.IsAny<InvestigationSession>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);

            _orchestrator = new InvestigationOrchestrator(
                _mockPlanClient.Object,
                _mockAnalysisClient.Object,
                _mockToolClient.Object,
                validator,
                mockSessionRepo.Object,
                _logger);
        }

        [Fact]
        public async Task InvestigateAsync_ShouldReturnResponse_WhenValidRequestProvided()
        {
            // Arrange
            var request = new InvestigationRequest(
                "trace-123",
                "case-456",
                "Find evidence of fraud",
                null,
                "user-789"
            );

            var plan = new PlanResponse
            {
                SummaryIntent = "Find evidence",
                InvestigationPlan = new List<PlanStep>
                {
                    new PlanStep
                    {
                        Step = 1,
                        ToolName = "list-pods",
                        Parameters = new Dictionary<string, object> { { "namespace", "default" } }
                    }
                }
            };

            var agentAnalysis = new AgentResponse
            {
                ReasoningSummary = "Test summary"
            };

            var toolRegistry = new List<ToolDefinitionDto>
            {
                new ToolDefinitionDto
                {
                    Name = "list-pods",
                    Description = "list pods",
                    IsIdempotent = true,
                    TimeoutSeconds = 30,
                    Parameters = new Dictionary<string, ToolParameterDto>()
                }
            };
            var namespaces = new List<string> { "default" };

            _mockPlanClient
                .Setup(c => c.GetPlanAsync(
                    request.Query,
                    It.IsAny<List<ToolDefinitionDto>>(),
                    It.IsAny<List<string>>(),
                    request.TraceId!,
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(plan);

            _mockToolClient
                .Setup(c => c.GetToolsAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(toolRegistry);

            _mockToolClient
                .Setup(c => c.GetNamespacesAsync("trace-123", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(namespaces);

            _mockToolClient
                .Setup(c => c.ExecuteToolAsync(
                    "list-pods",
                    It.IsAny<System.Text.Json.Nodes.JsonObject?>(),
                    "trace-123",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new System.Text.Json.Nodes.JsonObject { ["result"] = "ok" });

            _mockAnalysisClient
                .Setup(c => c.AnalyzeAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ToolResult>>(),
                    "trace-123",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(agentAnalysis);

            // Act
            var result = await _orchestrator.InvestigateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("trace-123", result.TraceId);
            Assert.Equal(InvestigationResponseStatus.Success, result.Status);
            Assert.Equal("Test summary", result.Summary);
            Assert.Single(result.ToolCalls);
            Assert.Equal("list-pods", result.ToolCalls[0].ToolName);
        }

        [Fact]
        public async Task InvestigateAsync_ShouldThrowArgumentException_WhenQueryIsNull()
        {
            // Arrange
            var request = new InvestigationQueryRequest(null, "case-456", null!, null, "user-789");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _orchestrator.InvestigateAsync(request));
        }

        [Fact]
        public async Task InvestigateAsync_ShouldCallPlanClient_WithCorrectParameters()
        {
            // Arrange
            var request = new InvestigationRequest(
                "trace-123",
                "case-456",
                "Query text",
                null,
                "user-789"
            );

            var plan = new PlanResponse
            {
                SummaryIntent = "Intent",
                InvestigationPlan = new List<PlanStep>
                {
                    new PlanStep
                    {
                        Step = 1,
                        ToolName = "list-pods",
                        Parameters = new Dictionary<string, object> { { "namespace", "default" } }
                    }
                }
            };

            var toolRegistry = new List<ToolDefinitionDto>
            {
                new ToolDefinitionDto
                {
                    Name = "list-pods",
                    Description = "list pods",
                    IsIdempotent = true,
                    TimeoutSeconds = 30,
                    Parameters = new Dictionary<string, ToolParameterDto>()
                }
            };
            var namespaces = new List<string> { "default" };

            _mockPlanClient
                .Setup(c => c.GetPlanAsync(
                    "Query text",
                    It.IsAny<List<ToolDefinitionDto>>(),
                    It.IsAny<List<string>>(),
                    "trace-123",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(plan);

            _mockToolClient
                .Setup(c => c.GetToolsAsync(It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(toolRegistry);

            _mockToolClient
                .Setup(c => c.GetNamespacesAsync("trace-123", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(namespaces);

            _mockToolClient
                .Setup(c => c.ExecuteToolAsync(
                    "list-pods",
                    It.IsAny<System.Text.Json.Nodes.JsonObject?>(),
                    "trace-123",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new System.Text.Json.Nodes.JsonObject { ["result"] = "ok" });

            _mockAnalysisClient
                .Setup(c => c.AnalyzeAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ToolResult>>(),
                    "trace-123",
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new AgentResponse { ReasoningSummary = "ok" });

            // Act
            await _orchestrator.InvestigateAsync(request);

            // Assert
            _mockPlanClient.Verify(
                c => c.GetPlanAsync(
                    "Query text",
                    It.IsAny<List<ToolDefinitionDto>>(),
                    It.IsAny<List<string>>(),
                    "trace-123",
                    It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);
        }
    }
}

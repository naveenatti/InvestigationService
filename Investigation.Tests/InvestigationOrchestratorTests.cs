using System.Threading.Tasks;
using Xunit;
using Moq;
using Investigation.Application.Orchestration;
using Investigation.Application.Contracts;
using Investigation.Domain;
using Investigation.Application.DTOs;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Investigation.Tests
{
    public class InvestigationOrchestratorTests
    {
        private readonly Mock<IAiAgentClient> _mockAiClient;
        private readonly Mock<ISessionRepository> _mockSessionRepository;
        private readonly Mock<ILogger<InvestigationOrchestrator>> _mockLogger;
        private readonly ActivitySource _activitySource;
        private readonly InvestigationOrchestrator _orchestrator;

        public InvestigationOrchestratorTests()
        {
            _mockAiClient = new Mock<IAiAgentClient>();
            _mockSessionRepository = new Mock<ISessionRepository>();
            _mockLogger = new Mock<ILogger<InvestigationOrchestrator>>();
            _activitySource = new ActivitySource("test");
            _orchestrator = new InvestigationOrchestrator(_mockAiClient.Object, _mockSessionRepository.Object, _activitySource, _mockLogger.Object);
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

            var agentResponse = new AgentResponse
            {
                Reasoning = "Test reasoning",
                ReasoningSummary = "Test summary",
                Actions = new List<AgentAction>
                {
                    new AgentAction { ToolName = "search_documents", Input = new { query = "fraud" } }
                }
            };

            _mockAiClient
                .Setup(c => c.InvestigateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync(agentResponse);

            // Act
            var result = await _orchestrator.InvestigateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("trace-123", result.TraceId);
            Assert.Equal(InvestigationResponseStatus.Success, result.Status);
            Assert.Equal("Test summary", result.Summary);
            Assert.Single(result.ToolCalls);
            Assert.Equal("search_documents", result.ToolCalls[0].ToolName);
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
        public async Task InvestigateAsync_ShouldCallAiClient_WithCorrectParameters()
        {
            // Arrange
            var request = new InvestigationRequest(
                "trace-123",
                "case-456",
                "Query text",
                null,
                "user-789"
            );

            var agentResponse = new AgentResponse { ReasoningSummary = "Summary", Actions = null };
            _mockAiClient
                .Setup(c => c.InvestigateAsync("Query text", "case-456", "trace-123", default))
                .ReturnsAsync(agentResponse);

            // Act
            await _orchestrator.InvestigateAsync(request);

            // Assert
            _mockAiClient.Verify(c => c.InvestigateAsync("Query text", "case-456", "trace-123", default), Times.Once);
        }
    }
}

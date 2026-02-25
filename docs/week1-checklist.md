# Investigation Service — Week 1 Completion Checklist

**Project:** Investigation Service  
**Target:** Clean Architecture, .NET 8 Web API  
**Date Completed:** 2026-02-25

---

## 🎯 Architecture Goals

- [x] Clean Architecture layering (Domain, Application, Infrastructure, API)
- [x] Dependency Injection (DI) registration
- [x] SOLID principles applied
- [x] Nullable reference types enabled
- [x] Async/await throughout

---

## ✅ Domain Layer

- [x] `InvestigationSession` entity with TraceId, CaseId, Query, Status
- [x] `InvestigationStatus` enum (Created, Running, Completed, Failed)
- [x] `AgentAction` domain model
- [x] `AgentResponse` domain model
- [x] `InvestigationStep` and `InvestigationStepStatus` entities
- [x] Session state management methods

---

## ✅ Application Layer — DTOs & Contracts

### Request/Response DTOs
- [x] `InvestigationRequest` record (TraceId, CaseId, Query, Context, UserId)
- [x] `InvestigationResponse` record (TraceId, CaseId, Status, Summary, Result, ToolCalls, DurationMs, TimestampUtc)
- [x] `ToolCallDto` record (ToolName, Status, DurationMs, Metadata)

### Service Contracts
- [x] `IAiAgentClient` interface
- [x] `ISessionRepository` interface
- [x] `IInvestigationOrchestrator` interface

---

## ✅ Application Layer — Orchestration

- [x] `InvestigationOrchestrator` implementation
- [x] Request validation (Query, CaseId, UserId required)
- [x] Activity span creation and tagging
- [x] AI agent invocation flow
- [x] Action-to-tool-call mapping
- [x] Structured logging (investigation.started, ai.agent.called, investigation.completed)
- [x] Duration capture and telemetry tags

---

## ✅ Infrastructure Layer

### Mock Implementations
- [x] `MockAiAgentClient` with hardcoded response
- [x] `InMemorySessionRepository` persists sessions
- [x] `PolicyFactory` for resilience patterns

### DI Registration
- [x] `IInvestigationOrchestrator` registered as scoped
- [x] `IAiAgentClient` registered to `MockAiAgentClient`
- [x] `ISessionRepository` registered as singleton
- [x] `ActivitySource` for OpenTelemetry tracing

---

## ✅ API Layer — Controller

- [x] `InvestigationController.Query()` endpoint
- [x] POST `/api/investigation/query` handler
- [x] Request validation before orchestrator call
- [x] TraceId extraction/generation from header
- [x] OpenTelemetry activity tagging
- [x] Proper exception handling (400, 500)
- [x] Logging (request received, completed, errors)
- [x] XML documentation comments for Swagger

---

## ✅ OpenTelemetry & Observability

- [x] `ActivitySource` named "InvestigationService"
- [x] Request/response activity spans
- [x] AI agent call span with client kind
- [x] Trace ID propagation
- [x] Tag population (traceId, caseId, userId, duration)
- [x] Error tagging on exceptions

---

## ✅ JsonNode Removal

- [x] Generic `JsonNode` replaced with typed DTOs
- [x] `InvestigationResponse` strongly typed
- [x] `ToolCallDto` strongly typed
- [x] `AgentResponse` strongly typed
- [x] Old `InvestigationResultDto` deprecated with comment

---

## ✅ Swagger / OpenAPI Documentation

- [x] POST endpoint documented with summary
- [x] Request body schema in Swagger UI
- [x] Response schemas (200, 400, 500)
- [x] XML comments on controller
- [x] XML comments on endpoint parameters
- [x] Example request/response in remarks
- [x] ProducesResponseType attributes

---

## ✅ Testing

- [x] Unit test skeleton: `InvestigationOrchestratorTests`
- [x] Test case: Valid request returns response
- [x] Test case: Null query throws ArgumentException
- [x] Test case: IAiAgentClient invoked with correct params
- [x] Mock setup for xUnit + Moq

---

## ✅ Build & Compilation

- [x] Investigation.Domain compiles
- [x] Investigation.Application compiles
- [x] Investigation.Infrastructure compiles
- [x] Investigation.API compiles
- [x] Investigation.Tests compiles (skeleton)
- [x] No warnings related to nullability or async
- [x] All projects reference correct dependencies

---

## ✅ Postman / API Testing

### Request Example
```
POST /api/investigation/query
X-Trace-Id: trace-123

{
  "traceId": "trace-123",
  "caseId": "case-001",
  "query": "Why is pod restarting?",
  "context": {
    "cluster": "prod"
  },
  "userId": "user-1"
}
```

### Response Example (200 OK)
```json
{
  "traceId": "trace-123",
  "caseId": "case-001",
  "status": "Success",
  "summary": "Mock reasoning for: Why is pod restarting?",
  "result": {
    "insight": "Investigation performed"
  },
  "toolCalls": [
    {
      "toolName": "search_documents",
      "status": "Pending",
      "durationMs": 0,
      "metadata": {
        "action": "search_documents",
        "input": { "query": "Why is pod restarting?", "limit": 5 }
      }
    }
  ],
  "durationMs": 120,
  "timestampUtc": "2026-02-25T12:00:00Z"
}
```

---

## 📋 Week-2 Roadmap

- [ ] Real AI Agent service integration (gRPC or HTTP)
- [ ] Tool Execution service implementation
- [ ] RAG (Retrieval-Augmented Generation) service
- [ ] Redis-based session persistence
- [ ] Database persistence layer
- [ ] Integration tests
- [ ] Docker Compose orchestration
- [ ] Kubernetes deployment manifests
- [ ] Performance optimization
- [ ] Circuit breaker pattern
- [ ] Retry strategy refinement

---

## 🔗 Key Files

```
Investigation.Domain/
├── InvestigationSession.cs
├── InvestigationStatus.cs
├── AgentResponse.cs
├── AgentAction.cs
└── InvestigationStep.cs

Investigation.Application/
├── Orchestration/
│   ├── IInvestigationOrchestrator.cs
│   └── InvestigationOrchestrator.cs
├── Contracts/
│   ├── InvestigationResponse.cs
│   └── IAiAgentClient.cs
└── DTOs/
    └── InvestigationRequest.cs

Investigation.Infrastructure/
├── Clients/
│   └── MockAiAgentClient.cs
└── DependencyInjection.cs

Investigation.API/
└── Controllers/
    └── InvestigationController.cs

Investigation.Tests/
└── InvestigationOrchestratorTests.cs
```

---

## ✨ Summary

All Week-1 deliverables completed:
1. ✅ Typed DTOs replacing JsonNode
2. ✅ Application Layer orchestration with validation
3. ✅ OpenTelemetry tracing and structured logging
4. ✅ Mock AI Agent integration
5. ✅ Strong API contract with Swagger documentation
6. ✅ Clean Architecture principles throughout
7. ✅ Unit test skeleton provided
8. ✅ Ready for Week-2 external service integration

**Status:** ✅ **READY FOR DEVELOPMENT**

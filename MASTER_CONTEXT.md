# InvestigationService: Master Context

## 1. Project Vision

We are building an **AI-powered Kubernetes Investigation Platform** that allows users to ask natural language questions about Kubernetes environments (e.g., "Why is payment-service crashing?") and receive structured, AI-generated investigation results.

### What Makes This Different

This is not just a tool runner. It is:

- **RAG-enhanced** – Context-aware reasoning
- **Multi-step AI reasoning** – Complex planning and execution
- **Deterministic execution-controlled** – Safe, auditable operations
- **Enterprise-safe investigation engine** – Production-ready

**Core Principle:** The LLM never directly executes infrastructure actions. All execution is controlled by a .NET layer.

---

## 2. High-Level Architecture

```
Frontend Layer
    |
    v
┌─────────────────────────────────────┐
│   API Layer (.NET)                  │
│   - Authentication                  │
│   - TraceId Generation              │
│   - Request Routing                 │
└──────────────┬──────────────────────┘
               |
               v
┌─────────────────────────────────────┐
│   Orchestration Layer (.NET)        │
│   - Coordinates execution           │
│   - Manages state                   │
│   - Handles retries                 │
└──────────────┬──────────────────────┘
               |
        ┌──────┴────────┐
        |               |
        v               v
┌──────────────┐  ┌──────────────────────┐
│ AI Agent     │  │ Tool Execution       │
│ (Python)     │  │ Service (.NET)       │
│              │  │                      │
│ - LLM        │  │ - Kubernetes SDK     │
│ - RAG        │  │ - Tool Execution     │
│ - Planning   │  │ - Retry Policies     │
│ - Summary    │  │ - Structured Results │
└──────┬───────┘  └──────────────────────┘
       |
       v
┌─────────────────────────────────────┐
│   Vector DB (RAG Knowledge)         │
└─────────────────────────────────────┘
```

---

## 3. Execution Flow

### User Query
```
"Why is payment-service crashing?"
```

### Step-by-Step Process

1. **Frontend Request** – Streamlit sends request to .NET API
2. **API Processing** – Generates traceId, handles authentication
3. **AI Planning** – Orchestrator calls AI Agent
4. **Context & Planning** – AI Agent:
   - Uses RAG to retrieve relevant context
   - Calls LLM to generate execution plan
   - Returns structured plan to orchestrator
5. **Tool Execution** – Orchestrator executes tools via Tool Execution Service
6. **Result Collection** – Raw results collected from all tools
7. **AI Analysis** – Results sent back to AI Agent for analysis
8. **Diagnosis Generation** – LLM generates structured diagnosis
9. **Response** – Structured response returned to frontend

---

## 4. Core Architectural Principles

### 🔒 Principle 1: LLM Never Executes Infrastructure

- Agent proposes plans only
- .NET layer executes them
- All infrastructure access is validated and logged

### 🧠 Principle 2: AI is Intelligence Layer Only

AI Agent responsibilities:
- Planning and reasoning
- Context interpretation
- Summarization and insight generation
- Confidence assessment

### ⚙️ Principle 3: .NET Controls Execution

.NET layer responsibilities:
- Deterministic execution
- Input validation
- Retry policies (Polly)
- Observability (structured logging)
- Authorization (future RBAC)
- TraceId propagation end-to-end

### 📚 Principle 4: RAG Provides Context

- Used before planning to inform agent decisions
- Optionally used after execution for verification
- Ensures decisions are based on knowledge base

---

## 5. Tech Stack

### Backend (.NET)
- **.NET 8** with async/await support
- **Clean Architecture** – Separation of concerns
- **Minimal APIs** or Controllers
- **Kubernetes SDK** – Native cluster communication
- **Polly** – Retry and resilience policies
- **Structured Logging** – TraceId propagation
- **ASP.NET Core** – Web framework

### AI Layer (Python)
- **FastAPI** – High-performance API
- **LLM Integration** – OpenAI / Azure OpenAI / Local LLM
- **RAG Pipeline** – Multi-stage retrieval and ranking
- **Vector DB** – Chroma / Pinecone / FAISS
- **Embedding Model** – State-of-the-art embeddings

### Frontend
- **Streamlit** – Simple investigation UI
- **Python** – Rapid development

---

## 6. Current Progress (Week 1 ✅ Complete)

### Completed
- ✅ Clean Architecture Investigation Service created
- ✅ Tool Execution Service scaffolded
- ✅ 5 Core APIs defined
- ✅ TraceId included in all contracts
- ✅ Retry handling with Polly
- ✅ Async support throughout
- ✅ Structured request/response contracts
- ✅ Clear separation between AI and execution layers

### Conceptual Tools Defined
- `list-pods` – Retrieve pod information
- `get-pod-logs` – Stream container logs
- `get-deployments` – List deployment status
- `get-resource-usage` – Monitor CPU/Memory
- `execute-command` – Run commands in pods

### Execution Engine Foundation
- Complete service infrastructure
- Contract definitions
- Logging framework
- Error handling patterns

---

## 7. Planned Phases

### Week 2 – Intelligence Layer
- Implement AI Agent service
- Implement RAG service and vector DB integration
- Define planning contract
- Define summarization contract
- Add structured investigation result schema
- Integrate LLM for:
  - Plan generation (multi-step reasoning)
  - Final summarization and diagnosis
  - Confidence scoring

### Week 3 – Advanced Agent Capabilities
- Reflection loop (re-plan if execution fails)
- Multi-step reasoning cycles
- Confidence scoring per result
- Failure recovery and retry strategies
- Tool capability discovery
- Observability dashboard
- Agent state persistence
- Caching & memory for common investigations

---

## 8. Core Features

### Natural Language Investigation
Users ask infrastructure questions in plain English without needing kubectl knowledge.

### Multi-Step Tool Planning
Agent generates structured execution plan with evidence collection strategy.

### Deterministic Tool Execution
All execution validated and controlled in .NET layer with full audit trail.

### Structured Diagnosis Output
Returns:
- Root cause analysis
- Supporting evidence
- Confidence level (0-1)
- Recommended remediation actions
- Tools used and their raw outputs

### Full Traceability
- TraceId end-to-end from request to response
- Execution logs for every step
- Step-by-step results
- Audit-ready compliance

---

## 9. Investigation Output Schema (Target)

```json
{
  "traceId": "trace-abc-123-def",
  "status": "completed",
  "rootCause": "Payment service is crashing due to OOMKilled.",
  "evidence": [
    "Pod restarted 6 times in 5 minutes",
    "Container logs show OutOfMemoryError",
    "Memory usage exceeded 512Mi limit",
    "Deployment has no memory requests/limits"
  ],
  "confidence": 0.91,
  "recommendedActions": [
    "Increase memory limits to 1Gi",
    "Investigate memory leaks in application",
    "Enable Horizontal Pod Autoscaler",
    "Review memory usage trends"
  ],
  "toolsUsed": [
    "list-pods",
    "get-pod-logs",
    "get-resource-usage"
  ],
  "executionTime": "2.345s",
  "stepResults": [
    {
      "step": 1,
      "tool": "list-pods",
      "status": "success",
      "result": { }
    }
  ]
}
```

---

## 10. Constraints & Requirements

- ❌ LLM must NOT execute infrastructure commands directly
- ✅ All tool execution must pass through .NET boundary
- ✅ Must be enterprise-safe (production-ready)
- ✅ Must support auditability (compliance-ready)
- ✅ Must support retries (resilient)
- ✅ Must support async execution (scalable)
- ✅ Must support future RBAC enforcement
- ✅ Must remain cloud-agnostic (portable)
- ✅ Must support structured error handling

---

## 11. Open Problems & Design Questions

### Execution Strategy
- [ ] Should agent support autonomous reflection loops for iterative problem-solving?
- [ ] Should execution be event-driven vs. request-driven?
- [ ] How to implement human-in-the-loop approval for sensitive operations?

### State Management
- [ ] Where should investigation state be stored (database, cache, in-memory)?
- [ ] Should we implement investigation caching for common queries?
- [ ] How to version investigation schema safely?

### Performance & Scale
- [ ] How to manage large log token limits for LLM context?
- [ ] Should we implement streaming LLM responses?
- [ ] Should we batch tool execution for efficiency?

### Safety & Compliance
- [ ] How to prevent prompt injection attacks?
- [ ] Should we implement a policy engine (OPA)?
- [ ] How to enforce tool execution policies?

---

## 12. What This System IS

✅ An **AI-augmented investigation engine** – Not just a wrapper
✅ A **controlled execution platform** – Safety by design
✅ A **reasoning + deterministic orchestration system** – Hybrid approach
✅ A **production-safe AI infrastructure layer** – Enterprise-ready
✅ An **auditability framework** – Compliance-ready

---

## 13. What This System IS NOT

❌ A direct LLM-to-Kubernetes bridge
❌ A chatbot with kubectl access
❌ A simple log summarizer
❌ A single-step tool caller
❌ An uncontrolled infrastructure mutator

---

## 14. Long-Term Vision

### Near Term (3-6 months)
- Autonomous investigation engine for common patterns
- Multi-cluster support
- Basic incident auto-diagnosis

### Medium Term (6-12 months)
- Production co-pilot with human-in-the-loop
- Advanced self-healing recommendations
- Cross-cluster correlation

### Long Term (12+ months)
- Autonomous SRE assistant
- Incident auto-remediation (with approval gates)
- Self-healing orchestration engine
- Predictive issue detection
- Enterprise-grade multi-tenant platform

---

## 15. One-Line Summary

> A RAG-enhanced AI investigation platform that uses LLM reasoning to generate multi-step execution plans, executed safely through a deterministic .NET control layer, returning structured infrastructure diagnoses with full auditability and compliance support.

---

## Quick Links & References

- **API Contracts:** See [Investigation.API](Investigation.API)
- **Domain Models:** See [Investigation.Domain](Investigation.Domain)
- **Infrastructure Clients:** See [Investigation.Infrastructure/Clients](Investigation.Infrastructure/Clients)
- **Orchestration Logic:** See [Investigation.Application/Orchestration](Investigation.Application/Orchestration)

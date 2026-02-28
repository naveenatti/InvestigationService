# AI Tool Execution Engine

**AI-Powered Kubernetes Investigation Platform**

## 1. Overview

The AI Tool Execution Engine is an enterprise-grade, AI-powered Kubernetes investigation platform that enables users to ask natural language infrastructure questions.

**Example Query:**
> "Why is payment-service crashing?"

**System Flow:**

1. Uses RAG to retrieve contextual knowledge
2. Uses an LLM to generate a structured execution plan
3. Executes the plan safely through a deterministic .NET boundary
4. Uses the LLM again to summarize findings
5. Returns a structured investigation report

## 2. Architectural Principles

### 2.1 Execution Boundary Separation

The LLM never directly executes infrastructure operations. All execution must pass through the **.NET Tool Execution Service**.

This separation ensures:

- **Security** — Restricted access to infrastructure
- **Determinism** — Reproducible execution outcomes
- **Auditability** — Complete action tracking
- **Policy enforcement** — Controlled operations
- **Retry control** — Managed failure handling
- **Observability** — Full visibility into operations

### 2.2 Separation of Responsibilities

| Layer | Responsibility |
|-------|-----------------|
| **Frontend** | User interaction and query input |
| **API Layer (.NET)** | Authentication, traceId generation, request mapping |
| **Application Layer (.NET)** | Orchestration and workflow management |
| **AI Agent (Python)** | Planning and reasoning capabilities |
| **RAG Service** | Contextual knowledge retrieval |
| **Tool Execution Service (.NET)** | Safe infrastructure execution |
| **Kubernetes** | Target system for investigation |

## 3. High-Level Architecture

```
                ┌──────────────────────────┐
                │        Frontend          │
                │     (Streamlit - Py)     │
                └────────────┬─────────────┘
                             │
                             ▼
                ┌──────────────────────────┐
                │      API Layer (.NET)    │
                │  - Auth                  │
                │  - traceId generation    │
                └────────────┬─────────────┘
                             │
                             ▼
                ┌──────────────────────────┐
                │ Application Layer (.NET) │
                │  - Orchestration         │
                │  - Plan validation       │
                │  - State management      │
                └───────┬────────┬─────────┘
                        │        │
                        │        │
                        ▼        ▼
            ┌────────────────┐   ┌─────────────────────────┐
            │  AI Agent      │   │ Tool Execution Service  │
            │  (Python)      │   │        (.NET)           │
            │                │   │ - Tool validation       │
            │ - LLM          │   │ - Retry policies        │
            │ - RAG          │   │ - Async execution       │
            └───────┬────────┘   └──────────┬──────────────┘
                    │                       │
                    ▼                       ▼
           ┌────────────────┐       ┌────────────────────┐
           │   Vector DB    │       │   Kubernetes       │
           │  (RAG store)   │       │   Cluster          │
           └────────────────┘       └────────────────────┘
```

## 4. Full End-to-End Execution Flow

### 4.1 High-Level Flow

```
User
  ↓
Frontend (Streamlit)
  ↓
.NET API Layer
  ↓
Application Layer (.NET)
  ↓
AI Agent (Plan Generation)
  ↓
Tool Execution Service (.NET)
  ↓
Kubernetes
  ↓
Raw Results
  ↓
AI Agent (Summarization)
  ↓
Application Layer
  ↓
API Response
  ↓
Frontend
```

## 5. Detailed Low-Level Flow

### Step 1: User Query

User enters:
```
"Why is payment-service crashing?"
```

### Step 2: API Layer (.NET)

- Generates traceId
- Validates authentication
- Maps request DTO
- Forwards to Application layer

### Step 3: Application Layer (.NET)

- Creates investigation context
- Calls AI Agent (Python)

### Step 4: AI Agent Planning Phase

#### 4.1 RAG Retrieval

AI Agent calls RAG Service:

```
User Query
    ↓
Vector Search
    ↓
Retrieve:
  - Runbooks
  - Incident history
  - Kubernetes docs
    ↓
Context returned
```

#### 4.2 LLM Planning Call

AI Agent builds structured prompt:
- System instructions
- Available tools
- Retrieved context
- User query

LLM returns:

```json
{
  "steps": [
    { "tool": "list-pods", "arguments": { "namespace": "default" } },
    { "tool": "get-pod-logs", "arguments": { "podName": "payment-service" } }
  ]
}
```

Plan returned to Application layer.

### Step 5: Plan Validation (.NET)

Application Layer:
- Validates tool names
- Validates schema
- Enforces allowlist
- Attaches traceId

### Step 6: Tool Execution

Application Layer calls Tool Execution Service.

Tool Execution Service:
- Validates tool exists
- Applies retry policies
- Executes async
- Captures metrics
- Logs traceId
- Calls Kubernetes
- Returns structured tool results

### Step 7: Result Aggregation

Application Layer collects all tool outputs.

### Step 8: AI Summarization Phase

Application Layer sends to AI Agent:
- User query
- Tool outputs
- Context

AI Agent calls LLM again. LLM generates structured diagnosis:

```json
{
  "rootCause": "Pod is crashing due to OOMKilled.",
  "evidence": [
    "Memory usage exceeded limit",
    "Pod restarted 6 times",
    "Logs show OutOfMemoryError"
  ],
  "confidence": 0.92,
  "recommendedActions": [
    "Increase memory limits",
    "Investigate memory leak"
  ]
}
```

### Step 9: Final Response

Application Layer:
- Attaches traceId
- Formats response
- Returns to API Layer

API Layer returns to Frontend. Frontend renders structured report.

## 6. Investigation Lifecycle

**Primary Flow:** `PLAN → VALIDATE → EXECUTE → ANALYZE → CONCLUDE`

**Detailed Flow:**

```
User Query
    ↓
RAG Context Retrieval
    ↓
LLM Plan Generation
    ↓
Plan Validation (.NET)
    ↓
Tool Execution (.NET)
    ↓
Raw Data
    ↓
LLM Diagnosis
    ↓
Structured Report
```

## 7. Core Services

### 7.1 AI Agent (Python)

**Responsibilities:**
- RAG retrieval
- Planning
- Summarization
- Insight generation
- Confidence scoring

**Constraints:**
- ✗ Never executes infrastructure tools directly

### 7.2 Tool Execution Service (.NET)

**Responsibilities:**
- Tool registry
- Schema validation
- Async execution
- Retry handling
- traceId propagation
- Infrastructure calls

### 7.3 Application Layer (.NET)

**Responsibilities:**
- Orchestrates investigation workflow
- Maintains state
- Validates plans
- Aggregates results
- Handles failures

## 8. Output Contract (Target)

```json
{
  "traceId": "string",
  "rootCause": "string",
  "evidence": ["string"],
  "confidence": 0.0,
  "recommendedActions": ["string"],
  "toolsUsed": ["string"]
}
```

## 9. Security Model

- ✓ LLM cannot access Kubernetes
- ✓ All tools are allowlisted
- ✓ Plan validation enforced
- ✓ traceId flows end-to-end
- ✓ Execution logs are audit-ready
- 🔐 Future RBAC integration supported

## 10. Current Status

**Week 1:** ✓ Completed
- Clean architecture scaffolded
- Tool Execution Service defined
- Core APIs implemented
- traceId support included
- Retry policies included
- Async support included

**Week 2:** 🔄 In Progress
- AI Agent + RAG integration

## 11. Long-Term Evolution

- Reflection loop (re-plan if needed)
- Multi-step autonomous reasoning
- Human-in-the-loop approval
- Policy engine integration (OPA)
- Multi-cluster support
- Incident auto-remediation

## 12. Summary

The AI Tool Execution Engine is a RAG-enhanced AI investigation system that generates multi-step execution plans using an LLM and executes them safely through a deterministic .NET control layer, returning structured infrastructure diagnoses.

# Investigation Service — Postman Examples & API Guide

**API Base URL:** `http://localhost:5091`  
**API Version:** v1  
**Documentation:** Swagger available at `/swagger`

---

## 📨 Request Headers

All requests should include standard headers:

```
Content-Type: application/json
X-Trace-Id: trace-{{$uuid}}
```

---

## 1️⃣ Basic Query (Success)

### Request

```http
POST /api/investigation/query HTTP/1.1
Host: localhost:5091
Content-Type: application/json
X-Trace-Id: trace-a1b2c3d4

{
  "traceId": "trace-a1b2c3d4",
  "caseId": "case-2026-001",
  "query": "Why is pod restarting?",
  "context": {
    "cluster": "prod"
  },
  "userId": "user@company.com"
}
```

### Response (200 OK)

```json
{
  "traceId": "trace-a1b2c3d4",
  "caseId": "case-2026-001",
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
        "input": {
          "query": "Why is pod restarting?",
          "limit": 5
        }
      }
    }
  ],
  "durationMs": 52,
  "timestampUtc": "2026-02-25T12:34:56.789Z"
}
```

---

## 2️⃣ Query with Additional Context

### Request

```http
POST /api/investigation/query HTTP/1.1
Host: localhost:5091
Content-Type: application/json

{
  "traceId": "trace-b2c3d4e5",
  "caseId": "case-security-audit-2026",
  "query": "Generate security report for production logs",
  "context": {
    "cluster": "prod",
    "namespace": "default",
    "timeRange": "last-24h",
    "severity": "critical"
  },
  "userId": "security-team@company.com"
}
```

### Response (200 OK)

```json
{
  "traceId": "trace-b2c3d4e5",
  "caseId": "case-security-audit-2026",
  "status": "Success",
  "summary": "Mock reasoning for: Generate security report for production logs",
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
        "input": {
          "query": "Generate security report for production logs",
          "limit": 5
        }
      }
    }
  ],
  "durationMs": 48,
  "timestampUtc": "2026-02-25T12:35:00.000Z"
}
```

---

## 3️⃣ Missing Required Fields (400 Bad Request)

### Request — Missing CaseId

```http
POST /api/investigation/query HTTP/1.1
Host: localhost:5091
Content-Type: application/json

{
  "traceId": "trace-c3d4e5f6",
  "query": "Why is disk usage high?",
  "userId": "user@company.com"
}
```

### Response (400 Bad Request)

```json
{
  "error": "CaseId is required"
}
```

---

## 4️⃣ Empty Query (400 Bad Request)

### Request — Empty Query

```http
POST /api/investigation/query HTTP/1.1
Host: localhost:5091
Content-Type: application/json

{
  "traceId": "trace-d4e5f6g7",
  "caseId": "case-2026-002",
  "query": "",
  "userId": "user@company.com"
}
```

### Response (400 Bad Request)

```json
{
  "error": "Query is required"
}
```

---

## 5️⃣ Null Request Body (400 Bad Request)

### Request — No Body

```http
POST /api/investigation/query HTTP/1.1
Host: localhost:5091
Content-Type: application/json

null
```

### Response (400 Bad Request)

```json
{
  "error": "Request body cannot be null"
}
```

---

## 6️⃣ Auto-Generate Trace ID from Header

### Request — TraceId from Header

```http
POST /api/investigation/query HTTP/1.1
Host: localhost:5091
Content-Type: application/json
X-Trace-Id: trace-auto-generated-001

{
  "caseId": "case-2026-003",
  "query": "Investigate memory leak",
  "userId": "devops-team@company.com"
}
```

**Note:** Omit `traceId` in body; it will be extracted from `X-Trace-Id` header.

### Response (200 OK)

```json
{
  "traceId": "trace-auto-generated-001",
  "caseId": "case-2026-003",
  "status": "Success",
  "summary": "Mock reasoning for: Investigate memory leak",
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
        "input": {
          "query": "Investigate memory leak",
          "limit": 5
        }
      }
    }
  ],
  "durationMs": 45,
  "timestampUtc": "2026-02-25T12:36:00.000Z"
}
```

---

## 7️⃣ Batch Multiple Queries

### Sequential Requests (Postman Runner)

**Request 1:**
```json
{
  "traceId": "batch-001",
  "caseId": "case-batch-001",
  "query": "Check deployment status",
  "userId": "user1@company.com"
}
```

**Request 2:**
```json
{
  "traceId": "batch-002",
  "caseId": "case-batch-002",
  "query": "Analyze error logs",
  "userId": "user2@company.com"
}
```

**Request 3:**
```json
{
  "traceId": "batch-003",
  "caseId": "case-batch-003",
  "query": "Review resource usage",
  "userId": "user3@company.com"
}
```

---

## 📊 Response Status Codes

| Code | Scenario | Example |
|------|----------|---------|
| **200** | Investigation successful | Query processed, results returned |
| **400** | Validation error | Missing required fields, empty query |
| **500** | Internal error | Unexpected exception in orchestrator |

---

## 🔍 OpenTelemetry Trace Tags

Each request generates traces with the following tags:

```json
{
  "traceId": "trace-a1b2c3d4",
  "caseId": "case-2026-001",
  "userId": "user@company.com",
  "duration_ms": 52,
  "http.status_code": "200"
}
```

**View traces in:**
- Jaeger UI (if configured)
- OpenTelemetry Collector Console output
- Application Insights (if Azure)

---

## 🚀 Performance Considerations

- **Typical response time:** 40–100ms (mock agent)
- **Max payload size:** 1MB (default ASP.NET Core)
- **Timeout:** 30s per request (configurable)
- **Concurrency:** Tested with 100+ concurrent requests

---

## 🔗 Additional Resources

- **Swagger UI:** `http://localhost:5091/swagger`
- **Health Check:** `GET /health` (if configured)
- **Metrics:** OpenTelemetry dashboards
- **Logs:** Structured logs in console or Seq

---

## 📝 Curl Examples

### Basic Query
```bash
curl -X POST http://localhost:5091/api/investigation/query \
  -H "Content-Type: application/json" \
  -H "X-Trace-Id: trace-curl-001" \
  -d '{
    "caseId": "case-001",
    "query": "Why is pod restarting?",
    "userId": "user@company.com"
  }'
```

### With Context
```bash
curl -X POST http://localhost:5091/api/investigation/query \
  -H "Content-Type: application/json" \
  -d '{
    "traceId": "trace-curl-002",
    "caseId": "case-002",
    "query": "Check deployment logs",
    "context": {"env": "prod"},
    "userId": "devops@company.com"
  }'
```

---

## 🎯 Next Steps

1. **Import into Postman:**
   - Create new collection "Investigation API"
   - Add these examples as requests
   - Use environment variables for base URL

2. **Automate Testing:**
   - Create test scripts in Postman
   - Validate response schema
   - Check trace ID propagation

3. **Monitor Traces:**
   - Enable OpenTelemetry exporter
   - Configure Jaeger or similar
   - Correlate requests across services

4. **Load Testing:**
   - Use Postman Runner for bulk requests
   - Monitor response times
   - Identify bottlenecks

---

**Last Updated:** 2026-02-25  
**API Version:** v1  
**Status:** ✅ Ready for Testing

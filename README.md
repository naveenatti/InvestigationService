# InvestigationService

This repository implements a production-ready Investigation Service using .NET 8, ASP.NET Core Web API, Clean Architecture, CQRS (MediatR), OpenTelemetry, Serilog, Polly, and HttpClientFactory.

## Quick curl example

```bash
curl -X POST http://localhost:5000/api/investigation/query \
  -H "Content-Type: application/json" \
  -H "X-Trace-Id: my-trace-id-123" \
  -d '{ "query": "investigate suspicious activity", "sessionId": "", "other": "meta" }'
```

Example request body (JSON):

```json
{
  "query": "string",
  "sessionId": "",
  "traceId": "optional"
}
```

## Docker

Build and run with Docker:

```bash
docker build -t investigation.api -f Investigation.API/Dockerfile .
docker run -p 5000:80 --env-file .env investigation.api
```

## Configuration

See `Investigation.API/appsettings.json` for example external service endpoints and Serilog/OpenTelemetry configuration.

## Notes

- The service propagates `X-Trace-Id` header to downstream services and emits OpenTelemetry spans.
- HttpClient retry policies are configured using Polly in `Investigation.Infrastructure`.
- The session repository is in-memory (`InMemorySessionRepository`) for now; swap with a persistent store via DI.

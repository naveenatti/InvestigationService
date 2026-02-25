using System;
using System.Collections.Generic;

namespace Investigation.Domain
{
    // Entity representing an investigation session
    public class InvestigationSession
    {
        public Guid Id { get; private set; }
        public string? Owner { get; private set; }
        public string? TraceId { get; private set; }
        public string? CaseId { get; private set; }
        public string? Query { get; private set; }
        public InvestigationStatus Status { get; private set; }
        public List<InvestigationStep> Steps { get; private set; } = new();
        public DateTime CreatedAt { get; private set; }

        public InvestigationSession(Guid id, string? owner, string? traceId = null, string? caseId = null, string? query = null)
        {
            Id = id;
            Owner = owner;
            TraceId = traceId;
            CaseId = caseId;
            Query = query;
            Status = InvestigationStatus.Created;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddStep(InvestigationStep step)
        {
            Steps.Add(step);
        }

        public void MarkCompleted() => Status = InvestigationStatus.Completed;
        public void MarkFailed() => Status = InvestigationStatus.Failed;
    }
}

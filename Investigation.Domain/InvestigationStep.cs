using System;

namespace Investigation.Domain
{
    // Represents a single step in the investigation orchestration
    public class InvestigationStep
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public InvestigationStepStatus Status { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public string? Details { get; private set; }

        public InvestigationStep(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Status = InvestigationStepStatus.Pending;
            StartedAt = DateTime.UtcNow;
        }

        public void MarkSuccess(string? details = null)
        {
            Status = InvestigationStepStatus.Success;
            Details = details;
            CompletedAt = DateTime.UtcNow;
        }

        public void MarkFailed(string? details = null)
        {
            Status = InvestigationStepStatus.Failed;
            Details = details;
            CompletedAt = DateTime.UtcNow;
        }
    }
}

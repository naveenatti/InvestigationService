using System;
using System.Collections.Generic;

namespace Investigation.Domain
{
    // Entity representing an investigation session
    public class InvestigationSession
    {
        public Guid Id { get; private set; }
        public string? Owner { get; private set; }
        public List<InvestigationStep> Steps { get; private set; } = new();
        public DateTime CreatedAt { get; private set; }

        public InvestigationSession(Guid id, string? owner)
        {
            Id = id;
            Owner = owner;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddStep(InvestigationStep step)
        {
            Steps.Add(step);
        }
    }
}

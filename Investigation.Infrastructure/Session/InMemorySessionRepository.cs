using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Domain;

namespace Investigation.Infrastructure.Session
{
    // Simple in-memory session repository for demo and testing
    public class InMemorySessionRepository : ISessionRepository
    {
        private readonly ConcurrentDictionary<Guid, InvestigationSession> _store = new();

        public Task<InvestigationSession?> GetAsync(Guid id, CancellationToken ct = default)
        {
            _store.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }

        public Task SaveAsync(InvestigationSession session, CancellationToken ct = default)
        {
            _store[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Domain;

namespace Investigation.Application.Contracts
{
    public interface ISessionRepository
    {
        Task<InvestigationSession?> GetAsync(Guid id, CancellationToken ct = default);
        Task SaveAsync(InvestigationSession session, CancellationToken ct = default);
    }
}

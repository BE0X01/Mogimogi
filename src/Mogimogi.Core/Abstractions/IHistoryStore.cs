using Mogimogi.Core.Models;

namespace Mogimogi.Core.Abstractions;

public interface IHistoryStore
{
    Task<IReadOnlyList<AnswerAttempt>> ReadAsync(CancellationToken cancellationToken = default);
    Task SaveAttemptAsync(AnswerAttempt attempt, CancellationToken cancellationToken = default);
}

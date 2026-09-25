using QuestionBank.Core.Models;

namespace QuestionBank.Core.Abstractions;

public interface IHistoryStore
{
    Task<IReadOnlyList<AnswerAttempt>> ReadAsync(CancellationToken cancellationToken = default);
    Task SaveAttemptAsync(AnswerAttempt attempt, CancellationToken cancellationToken = default);
}

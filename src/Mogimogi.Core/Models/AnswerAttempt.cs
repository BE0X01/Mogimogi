using System.Text.Json.Serialization;

namespace Mogimogi.Core.Models;

// 당시 문제와 선지 순서도 저장하므로 원본이 바뀌어도 과거 풀이를 해석할 수 있습니다.
public sealed record AnswerAttempt(
    Guid AttemptId,
    Guid SessionId,
    string QuestionId,
    string ContentHash,
    string BookId,
    string BookName,
    string QuestionText,
    AnswerOption[] DisplayOptions,
    string SelectedAnswerId,
    string CorrectAnswerId,
    DateTimeOffset AnsweredAt)
{
    [JsonIgnore]
    public bool IsCorrect => SelectedAnswerId == CorrectAnswerId;
}

public sealed record QuestionStatistics(string QuestionId, int CorrectCount, int WrongCount,
    bool LastAnswerCorrect, DateTimeOffset LastAnsweredAt);

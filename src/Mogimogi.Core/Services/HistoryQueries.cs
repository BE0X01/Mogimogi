using Mogimogi.Core.Models;

namespace Mogimogi.Core.Services;

public static class HistoryQueries
{
    public static IReadOnlyDictionary<string, QuestionStatistics> Summarize(IEnumerable<AnswerAttempt> attempts)
    {
        return attempts.GroupBy(a => a.QuestionId).ToDictionary(g => g.Key, g =>
        {
            var last = g.OrderBy(a => a.AnsweredAt).Last();
            return new QuestionStatistics(g.Key, g.Count(a => a.IsCorrect), g.Count(a => !a.IsCorrect),
                last.IsCorrect, last.AnsweredAt);
        });
    }

    // 다음 버전의 '오답만 풀기': 가장 최근 풀이가 오답인 문제를 대상으로 합니다.
    public static HashSet<string> LatestWrongIds(IEnumerable<AnswerAttempt> attempts)
    {
        return Summarize(attempts).Values.Where(s => !s.LastAnswerCorrect)
            .Select(s => s.QuestionId).ToHashSet(StringComparer.Ordinal);
    }
}

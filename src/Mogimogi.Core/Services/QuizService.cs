using Mogimogi.Core.Abstractions;
using Mogimogi.Core.Models;

namespace Mogimogi.Core.Services;

public sealed class QuizService(IHistoryStore historyStore, Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;

    // 여러 문제집과 오답 필터를 지원합니다. 확장 화면에서도 이 출제 로직을 재사용합니다.
    public QuizSession CreateSession(IEnumerable<QuestionBook> books, int count,
        IReadOnlySet<string>? allowedQuestionIds = null)
    {
        var candidates = books.SelectMany(b => b.Questions.Select(q => (Book: b, Question: q)))
            .Where(x => allowedQuestionIds is null || allowedQuestionIds.Contains(x.Question.Id))
            .GroupBy(x => x.Question.Id, StringComparer.Ordinal).Select(g =>
            {
                if (g.Select(x => x.Question.ContentHash).Distinct().Count() != 1)
                {
                    throw new InvalidDataException($"서로 다른 문제에 같은 QuestionID가 있습니다: {g.Key}");
                }
                return g.First();
            }).ToArray();
        if (count < 1 || count > candidates.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"문제 수는 1-{candidates.Length} 사이여야 합니다.");
        }
        Shuffle(candidates);
        var items = candidates.Take(count).Select(x =>
        {
            var options = x.Question.Options.ToArray();
            Shuffle(options);
            return new QuizItem(x.Book.Id, x.Book.Name, x.Question, Array.AsReadOnly(options));
        }).ToArray();
        return new QuizSession(items, historyStore);
    }

    private void Shuffle<T>(T[] items)
    {
        for (var i = items.Length - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}

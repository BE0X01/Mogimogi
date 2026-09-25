using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuestionBank.Core.Models;

public sealed record AnswerOption(string Id, string Text);

public sealed class Question
{
    public string Id { get; }
    public string Text { get; }
    public IReadOnlyList<AnswerOption> Options { get; }
    public string CorrectAnswerId { get; }
    public string ContentHash { get; }

    public Question(string text, IEnumerable<AnswerOption> options, string correctAnswerId,
        string? explicitId = null)
    {
        Text = Normalize(text);
        var choices = options.Select(o => new AnswerOption(o.Id.Trim().ToUpperInvariant(), Normalize(o.Text)))
            .OrderBy(o => o.Id, StringComparer.Ordinal).ToArray();
        CorrectAnswerId = correctAnswerId.Trim().ToUpperInvariant();
        if (Text.Length == 0 || choices.Length != 4 || choices.Any(o => o.Text.Length == 0))
        {
            throw new ArgumentException("문제와 선지 4개를 모두 입력해야 합니다.");
        }
        if (!choices.Select(o => o.Id).SequenceEqual(new[] { "AS1", "AS2", "AS3", "AS4" }))
        {
            throw new ArgumentException("선지 ID는 AS1, AS2, AS3, AS4여야 합니다.");
        }
        if (!choices.Any(o => o.Id == CorrectAnswerId))
        {
            throw new ArgumentException("정답ID에는 AS1, AS2, AS3, AS4 중 하나를 입력하세요.");
        }
        Options = Array.AsReadOnly(choices);
        // 파일명·행 번호는 해시에 포함하지 않아 이동/정렬로 ID가 바뀌지 않습니다.
        var canonical = JsonSerializer.Serialize(new { Text, Options, CorrectAnswerId });
        ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        Id = string.IsNullOrWhiteSpace(explicitId) ? $"AUTO-{ContentHash}" : explicitId.Trim();
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim().Normalize();
    }
}

public sealed record QuestionBook(string Id, string Category, string Name, string FilePath,
    IReadOnlyList<Question> Questions);

public sealed record QuizItem(string BookId, string BookName, Question Question,
    IReadOnlyList<AnswerOption> DisplayOptions);

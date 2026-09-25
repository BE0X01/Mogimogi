using QuestionBank.Core.Abstractions;
using QuestionBank.Core.Models;

namespace QuestionBank.Core.Services;

public sealed class QuizSession
{
    private readonly IReadOnlyList<QuizItem> _items;
    private readonly IHistoryStore _historyStore;
    private readonly List<AnswerAttempt> _answers = [];
    private readonly SemaphoreSlim _submitGate = new(1, 1);
    private AnswerAttempt? _pendingAttempt;

    public Guid Id { get; } = Guid.NewGuid();
    public int Index { get; private set; }
    public int Total => _items.Count;
    public bool IsCompleted => Index >= Total;
    public bool CurrentAnswered => !IsCompleted && _answers.Count > Index;
    public bool HasPendingSave => _pendingAttempt is not null;
    public QuizItem Current => !IsCompleted ? _items[Index] : throw new InvalidOperationException("풀이가 끝났습니다.");
    public IReadOnlyList<AnswerAttempt> Answers => _answers.AsReadOnly();

    internal QuizSession(QuizItem[] items, IHistoryStore historyStore)
    {
        _items = Array.AsReadOnly(items);
        _historyStore = historyStore;
    }

    public async Task<AnswerAttempt> SubmitAsync(string selectedAnswerId)
    {
        await _submitGate.WaitAsync();
        try
        {
            if (CurrentAnswered || IsCompleted)
            {
                throw new InvalidOperationException("이미 제출한 문제입니다.");
            }
            var item = Current;
            if (!item.DisplayOptions.Any(o => o.Id == selectedAnswerId))
            {
                throw new ArgumentException("표시된 선지 중 하나를 선택하세요.");
            }
            if (_pendingAttempt is not null && _pendingAttempt.SelectedAnswerId != selectedAnswerId)
            {
                throw new InvalidOperationException("저장 재시도 중에는 답안을 바꿀 수 없습니다.");
            }
            _pendingAttempt ??= new AnswerAttempt(Guid.NewGuid(), Id, item.Question.Id,
                item.Question.ContentHash, item.BookId, item.BookName, item.Question.Text,
                item.DisplayOptions.ToArray(), selectedAnswerId, item.Question.CorrectAnswerId, DateTimeOffset.UtcNow);
            // 저장에 실패하면 진행 상태와 점수를 변경하지 않습니다. 재시도는 같은 AttemptId를 씁니다.
            await _historyStore.SaveAttemptAsync(_pendingAttempt);
            var saved = _pendingAttempt;
            _answers.Add(saved);
            _pendingAttempt = null;
            return saved;
        }
        finally
        {
            _submitGate.Release();
        }
    }

    public void MoveNext()
    {
        if (!CurrentAnswered)
        {
            throw new InvalidOperationException("답안을 제출한 뒤 다음 문제로 이동하세요.");
        }
        Index++;
    }
}

using System.Text.Encodings.Web;
using System.Text.Json;
using Mogimogi.Core.Abstractions;
using Mogimogi.Core.Models;

namespace Mogimogi.Infrastructure;

public sealed class JsonHistoryStore : IHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonHistoryStore(string path)
    {
        _path = Path.GetFullPath(path);
    }

    public async Task<IReadOnlyList<AnswerAttempt>> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var fileLock = AcquireFileLock();
            return (await LoadAsync(cancellationToken)).Attempts.AsReadOnly();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAttemptAsync(AnswerAttempt attempt, CancellationToken cancellationToken = default)
    {
        ValidateAttempt(attempt);
        await _gate.WaitAsync(cancellationToken);
        string? temporaryPath = null;
        try
        {
            using var fileLock = AcquireFileLock();
            var document = await LoadAsync(cancellationToken);
            var previous = document.Attempts.SingleOrDefault(a => a.AttemptId == attempt.AttemptId);
            if (previous is not null)
            {
                if (JsonSerializer.Serialize(previous, JsonOptions) != JsonSerializer.Serialize(attempt, JsonOptions))
                {
                    throw new InvalidDataException("같은 풀이 ID에 다른 답안이 있습니다.");
                }
                return;
            }
            document.Attempts.Add(attempt);
            temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            // 같은 디렉터리의 임시 파일을 교체합니다. 기존 기록은 .bak에 한 버전 보관합니다.
            if (File.Exists(_path))
            {
                File.Replace(temporaryPath, _path, _path + ".bak");
            }
            else
            {
                File.Move(temporaryPath, _path);
            }
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            _gate.Release();
        }
    }

    private FileStream AcquireFileLock()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        return new FileStream(_path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private async Task<HistoryDocument> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new HistoryDocument { SchemaVersion = 1, Attempts = [] };
        }
        try
        {
            await using var stream = File.OpenRead(_path);
            var document = await JsonSerializer.DeserializeAsync<HistoryDocument>(stream, JsonOptions, cancellationToken);
            if (document is null || document.SchemaVersion != 1 || document.Attempts is null)
            {
                throw new InvalidDataException("지원하지 않거나 손상된 기록 형식입니다.");
            }
            var ids = new HashSet<Guid>();
            foreach (var attempt in document.Attempts)
            {
                ValidateAttempt(attempt);
                if (!ids.Add(attempt.AttemptId))
                {
                    throw new InvalidDataException("중복 풀이 ID가 있습니다.");
                }
            }
            return document;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException)
        {
            throw new InvalidDataException($"풀이 기록을 읽지 못했습니다. 기존 파일은 변경하지 않았습니다.\n{_path}\n" +
                "프로그램을 닫고 원본을 복사해 보관한 뒤 history.json.bak 복원을 확인하세요.", ex);
        }
    }

    private static void ValidateAttempt(AnswerAttempt? attempt)
    {
        if (attempt is null || attempt.AttemptId == Guid.Empty || attempt.SessionId == Guid.Empty ||
            string.IsNullOrWhiteSpace(attempt.QuestionId) || string.IsNullOrWhiteSpace(attempt.ContentHash) ||
            string.IsNullOrWhiteSpace(attempt.BookId) || string.IsNullOrWhiteSpace(attempt.BookName) ||
            string.IsNullOrWhiteSpace(attempt.QuestionText) || attempt.AnsweredAt == default ||
            attempt.DisplayOptions is null || attempt.DisplayOptions.Length != 4 ||
            attempt.DisplayOptions.Any(o => o is null || string.IsNullOrWhiteSpace(o.Text)) ||
            !attempt.DisplayOptions.Select(o => o.Id).Order(StringComparer.Ordinal)
                .SequenceEqual(new[] { "AS1", "AS2", "AS3", "AS4" }) ||
            !attempt.DisplayOptions.Any(o => o.Id == attempt.SelectedAnswerId) ||
            !attempt.DisplayOptions.Any(o => o.Id == attempt.CorrectAnswerId))
        {
            throw new InvalidDataException("풀이 기록의 필수 항목이 잘못되었습니다.");
        }
    }

    private sealed class HistoryDocument
    {
        public required int SchemaVersion { get; init; }
        public required List<AnswerAttempt> Attempts { get; init; }
    }
}

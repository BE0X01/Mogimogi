using ClosedXML.Excel;
using QuestionBank.Core.Abstractions;
using QuestionBank.Core.Models;
using QuestionBank.Core.Services;
using QuestionBank.Infrastructure;

var test = new Checks();
await test.RunAsync();

internal sealed class Checks
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "QuestionBank.Checks-" + Guid.NewGuid().ToString("N"));
    private int _passed;

    internal async Task RunAsync()
    {
        Directory.CreateDirectory(_root);
        try
        {
            await Check("동봉한 샘플 3개를 실제 ClosedXML로 읽음", () =>
            {
                var result = new QuestionBookCatalog(new ExcelQuestionBookReader()).Load(Path.Combine(AppContext.BaseDirectory, "Data"));
                Assert(result.Errors.Count == 0 && result.Books.Count == 3);
                Assert(result.Books.Sum(b => b.Questions.Count) == 20);
                Assert(result.Books.Select(b => b.Category).Distinct().Count() == 2);
            });
            await Check("헤더 있음/없음, 빈 행, AS ID 정규화", () =>
            {
                var a = WriteBook("head.xlsx", true, row => row.Cell(6).Value = " as1 ");
                var b = WriteBook("nohead.xlsx", false);
                Assert(Read(a).Questions.Count == 2 && Read(b).Questions.Count == 2);
                Assert(Read(a).Questions[0].CorrectAnswerId == "AS1");
            });
            await Check("파일 이름 및 행 순서 변경에도 자동 ID 유지", () =>
            {
                var source = WriteBook("original.xlsx", true);
                var before = Read(source).Questions.Select(q => q.Id).Order().ToArray();
                var path = Path.Combine(_root, "renamed.xlsx");
                using (var wb = new XLWorkbook(source))
                {
                    var sheet = wb.Worksheet(1);
                    sheet.Range("A2:G2").CopyTo(sheet.Cell("A5"));
                    sheet.Row(2).Clear();
                    wb.SaveAs(path);
                }
                Assert(before.SequenceEqual(Read(path).Questions.Select(q => q.Id).Order()));
            });
            await Check("선택 7열 고정 ID는 문구 수정 뒤에도 유지", () =>
            {
                var a = MakeQuestion("문제", "vocab-001");
                var b = MakeQuestion("수정된 문제", "vocab-001");
                Assert(a.Id == b.Id && a.ContentHash != b.ContentHash);
                var p = WriteBook("explicit.xlsx", true, r => r.Cell(7).Value = "vocab-001");
                Assert(Read(p).Questions[0].Id == "vocab-001");
            });
            await Check("빈 선지·잘못된 정답·수식·중복 ID는 행 번호 오류", () =>
            {
                foreach (var invalid in new Action<IXLRow>[]
                {
                    r => r.Cell(3).Clear(), r => r.Cell(6).Value = "1",
                    r => r.Cell(1).FormulaA1 = "1+1"
                })
                {
                    var path = WriteBook(Guid.NewGuid() + ".xlsx", true, invalid);
                    var error = Throws<InvalidDataException>(() => Read(path));
                    Assert(error.Message.Contains("2행"));
                }
                var duplicate = WriteBook("duplicate.xlsx", true);
                using (var wb = new XLWorkbook(duplicate))
                {
                    wb.Worksheet(1).Range("A2:G2").CopyTo(wb.Worksheet(1).Cell("A4"));
                    wb.Save();
                }
                Assert(Throws<InvalidDataException>(() => Read(duplicate)).Message.Contains("4행"));
            });
            await Check("첫 시트만 읽고 비어 있는 문제집 거부", () =>
            {
                var path = Path.Combine(_root, "empty.xlsx");
                using (var wb = new XLWorkbook())
                {
                    wb.AddWorksheet("empty");
                    wb.AddWorksheet("ignored").Cell(1, 1).Value = "뒤쪽 시트";
                    wb.SaveAs(path);
                }
                Throws<InvalidDataException>(() => Read(path));
            });
            await Check("손상 파일 격리, Excel 임시 파일 무시", () =>
            {
                var dir = Path.Combine(_root, "catalog");
                Directory.CreateDirectory(dir);
                File.Copy(WriteBook("valid.xlsx", true), Path.Combine(dir, "어휘_문제1.xlsx"));
                File.WriteAllText(Path.Combine(dir, "broken.xlsx"), "broken");
                File.WriteAllText(Path.Combine(dir, "~$temp.xlsx"), "temp");
                var result = new QuestionBookCatalog(new ExcelQuestionBookReader()).Load(dir);
                Assert(result.Books.Count == 1 && result.Errors.Count == 1);
            });
            await Check("문제집 간 고정 ID 충돌 시 양쪽 격리", () =>
            {
                var dir = Path.Combine(_root, "conflict");
                Directory.CreateDirectory(dir);
                File.Copy(WriteBook("ca.xlsx", true, r => r.Cell(7).Value = "shared"), Path.Combine(dir, "a.xlsx"));
                File.Copy(WriteBook("cb.xlsx", true, r =>
                {
                    r.Cell(7).Value = "shared";
                    r.Cell(1).Value = "다른 내용";
                }), Path.Combine(dir, "b.xlsx"));
                var result = new QuestionBookCatalog(new ExcelQuestionBookReader()).Load(dir);
                Assert(result.Books.Count == 0 && result.Errors.Count == 1);
            });
            await CheckAsync("복수 문제집 중복 제거 및 지정 수 출제", async () =>
            {
                var store = new MemoryStore();
                var bank = MakeBook(30);
                var session = new QuizService(store, new Random(1)).CreateSession([bank, bank], 30);
                var ids = new HashSet<string>();
                while (!session.IsCompleted)
                {
                    Assert(ids.Add(session.Current.Question.Id));
                    await session.SubmitAsync("AS1");
                    session.MoveNext();
                }
                Assert(ids.Count == 30 && store.Items.Count == 30);
            });
            await Check("문제 수 0/초과 거부 및 오답 필터", () =>
            {
                var service = new QuizService(new MemoryStore());
                var book = MakeBook(3);
                Throws<ArgumentOutOfRangeException>(() => service.CreateSession([book], 0));
                Throws<ArgumentOutOfRangeException>(() => service.CreateSession([book], 4));
                var ids = new HashSet<string> { book.Questions[1].Id };
                Assert(service.CreateSession([book], 1, ids).Current.Question.Id == book.Questions[1].Id);
            });
            await CheckAsync("선지 ID/텍스트 보존 및 모든 표시 위치에서 정답 채점", async () =>
            {
                var slots = new HashSet<int>();
                var orders = new HashSet<string>();
                var book = MakeBook(1);
                for (var seed = 0; seed < 64; seed++)
                {
                    var session = new QuizService(new MemoryStore(), new Random(seed)).CreateSession([book], 1);
                    var options = session.Current.DisplayOptions;
                    Assert(options.Select(o => o.Id).Distinct().Count() == 4);
                    Assert(options.All(o => book.Questions[0].Options.Contains(o)));
                    slots.Add(options.ToList().FindIndex(o => o.Id == "AS1"));
                    orders.Add(string.Join(',', options.Select(o => o.Id)));
                    Assert((await session.SubmitAsync("AS1")).IsCorrect);
                }
                Assert(slots.Count == 4 && orders.Count > 1);
            });
            await CheckAsync("미선택·중복 제출·미제출 이동 차단", async () =>
            {
                var session = new QuizService(new MemoryStore()).CreateSession([MakeBook(1)], 1);
                Throws<InvalidOperationException>(() => session.MoveNext());
                await ThrowsAsync<ArgumentException>(() => session.SubmitAsync("AS9"));
                Assert(!(await session.SubmitAsync("AS2")).IsCorrect);
                await ThrowsAsync<InvalidOperationException>(() => session.SubmitAsync("AS1"));
                Assert(session.Answers.Count == 1);
            });
            await CheckAsync("저장 실패 시 진행 유지, 같은 풀이 ID로 재시도", async () =>
            {
                var store = new FailOnceStore();
                var session = new QuizService(store).CreateSession([MakeBook(1)], 1);
                await ThrowsAsync<IOException>(() => session.SubmitAsync("AS1"));
                Assert(session.Index == 0 && session.Answers.Count == 0 && session.HasPendingSave);
                await ThrowsAsync<InvalidOperationException>(() => session.SubmitAsync("AS2"));
                await session.SubmitAsync("AS1");
                Assert(store.Received.Count == 2 && store.Received.Distinct().Count() == 1);
                Assert(session.Answers.Count == 1 && !session.HasPendingSave);
            });
            await CheckAsync("JSON 재시작 읽기·중복 저장 방지·백업 생성", async () =>
            {
                var path = Path.Combine(_root, "history.json");
                var store = new JsonHistoryStore(path);
                var session = new QuizService(store).CreateSession([MakeBook(2)], 2);
                var first = await session.SubmitAsync("AS1");
                await store.SaveAttemptAsync(first);
                session.MoveNext();
                await session.SubmitAsync("AS2");
                var loaded = await new JsonHistoryStore(path).ReadAsync();
                Assert(loaded.Count == 2 && File.Exists(path + ".bak"));
                Assert((await new JsonHistoryStore(path + ".bak").ReadAsync()).Count == 1);
                Assert(loaded.Single(a => a.AttemptId == first.AttemptId).QuestionText == first.QuestionText);
                Assert(!Directory.EnumerateFiles(_root, "*.tmp").Any());
            });
            await CheckAsync("손상·null·잘못된 스키마 기록을 덮어쓰지 않음", async () =>
            {
                var path = Path.Combine(_root, "corrupt.json");
                var store = new JsonHistoryStore(path);
                foreach (var content in new[] { "{broken", "null", "{}", "{\"schemaVersion\":9,\"attempts\":[]}", "{\"schemaVersion\":1,\"attempts\":[null]}" })
                {
                    await File.WriteAllTextAsync(path, content);
                    var session = new QuizService(store).CreateSession([MakeBook(1)], 1);
                    await ThrowsAsync<InvalidDataException>(() => session.SubmitAsync("AS1"));
                    Assert(await File.ReadAllTextAsync(path) == content);
                }
            });
            await CheckAsync("쓰기 불가 시 기존 JSON과 점수 보존", async () =>
            {
                var path = Path.Combine(_root, "locked.json");
                var store = new JsonHistoryStore(path);
                var session = new QuizService(store).CreateSession([MakeBook(2)], 2);
                await session.SubmitAsync("AS1");
                session.MoveNext();
                var original = await File.ReadAllTextAsync(path);
                using (var fileLock = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    await ThrowsAsync<IOException>(() => session.SubmitAsync("AS2"));
                }
                Assert(session.Answers.Count == 1 && await File.ReadAllTextAsync(path) == original);
                await session.SubmitAsync("AS2");
                Assert((await store.ReadAsync()).Count == 2);
            });
            await CheckAsync("정답/오답 횟수와 마지막 오답 집계", async () =>
            {
                var store = new MemoryStore();
                var book = MakeBook(1);
                await new QuizService(store).CreateSession([book], 1).SubmitAsync("AS2");
                Assert(HistoryQueries.LatestWrongIds(store.Items).Count == 1);
                await new QuizService(store).CreateSession([book], 1).SubmitAsync("AS1");
                var stats = HistoryQueries.Summarize(store.Items).Single().Value;
                Assert(stats.CorrectCount == 1 && stats.WrongCount == 1 && stats.LastAnswerCorrect);
                Assert(HistoryQueries.LatestWrongIds(store.Items).Count == 0);
            });
            Console.WriteLine($"\nPASS: {_passed} checks");
        }
        finally
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private Task Check(string name, Action action)
    {
        return CheckAsync(name, () => { action(); return Task.CompletedTask; });
    }

    private async Task CheckAsync(string name, Func<Task> action)
    {
        await action();
        _passed++;
        Console.WriteLine($"PASS {_passed:00}: {name}");
    }

    private string WriteBook(string name, bool header, Action<IXLRow>? change = null)
    {
        var path = Path.Combine(_root, name);
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("문제");
        if (header)
        {
            var headers = new[] { "문제", "선지1", "선지2", "선지3", "선지4", "정답ID", "QuestionID" };
            for (var c = 0; c < headers.Length; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
            }
        }
        var start = header ? 2 : 1;
        for (var i = 0; i < 2; i++)
        {
            var row = sheet.Row(start + i * 2);
            row.Cell(1).Value = "테스트 문제 " + i;
            for (var c = 1; c <= 4; c++)
            {
                row.Cell(c + 1).Value = "선지 " + c;
            }
            row.Cell(6).Value = "AS1";
        }
        change?.Invoke(sheet.Row(start));
        wb.SaveAs(path);
        return path;
    }

    private static QuestionBook Read(string path) => new ExcelQuestionBookReader().Read(path);
    private static Question MakeQuestion(string text, string? id = null) => new(text,
        Enumerable.Range(1, 4).Select(i => new AnswerOption($"AS{i}", $"선지 {i}")), "AS1", id);
    private static QuestionBook MakeBook(int count) => new("test.xlsx", "어휘", "테스트", "test.xlsx",
        Enumerable.Range(0, count).Select(i => MakeQuestion("문제 " + i)).ToArray());

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new Exception("Assertion failed");
        }
    }

    private static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T ex) { return ex; }
        throw new Exception($"Expected {typeof(T).Name}");
    }

    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}");
    }
}

internal class MemoryStore : IHistoryStore
{
    internal List<AnswerAttempt> Items { get; } = [];
    public Task<IReadOnlyList<AnswerAttempt>> ReadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AnswerAttempt>>(Items);
    }
    public virtual Task SaveAttemptAsync(AnswerAttempt attempt, CancellationToken cancellationToken = default)
    {
        Items.Add(attempt);
        return Task.CompletedTask;
    }
}

internal sealed class FailOnceStore : MemoryStore
{
    internal List<Guid> Received { get; } = [];
    public override Task SaveAttemptAsync(AnswerAttempt attempt, CancellationToken cancellationToken = default)
    {
        Received.Add(attempt.AttemptId);
        if (Received.Count == 1)
        {
            throw new IOException("Simulated disk failure");
        }
        return base.SaveAttemptAsync(attempt, cancellationToken);
    }
}

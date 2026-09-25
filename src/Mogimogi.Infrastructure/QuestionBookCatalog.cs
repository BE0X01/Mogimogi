using Mogimogi.Core.Abstractions;
using Mogimogi.Core.Models;

namespace Mogimogi.Infrastructure;

public sealed record CatalogResult(IReadOnlyList<QuestionBook> Books, IReadOnlyList<string> Errors);

public sealed class QuestionBookCatalog(IQuestionBookReader reader)
{
    public CatalogResult Load(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        var books = new List<QuestionBook>();
        var errors = new List<string>();
        foreach (var path in Directory.EnumerateFiles(dataDirectory)
            .Where(p => Path.GetExtension(p).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            .Where(p => !Path.GetFileName(p).StartsWith("~$", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                books.Add(reader.Read(path));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // 한 파일이 잘못되어도 정상 파일은 사용할 수 있습니다.
                errors.Add($"{Path.GetFileName(path)}\n{ex.Message}");
            }
        }
        var conflicts = books.SelectMany(b => b.Questions.Select(q => (Book: b, Question: q)))
            .GroupBy(x => x.Question.Id, StringComparer.Ordinal)
            .Where(g => g.Select(x => x.Question.ContentHash).Distinct().Count() > 1).ToArray();
        var invalidBooks = new HashSet<string>(StringComparer.Ordinal);
        foreach (var conflict in conflicts)
        {
            var names = conflict.Select(x => x.Book.Name).Distinct().ToArray();
            errors.Add($"QuestionID 충돌: {conflict.Key}\n내용이 다른 문제가 같은 ID를 사용합니다: {string.Join(", ", names)}");
            foreach (var item in conflict)
            {
                invalidBooks.Add(item.Book.Id);
            }
        }
        books.RemoveAll(b => invalidBooks.Contains(b.Id));
        return new CatalogResult(books.AsReadOnly(), errors.AsReadOnly());
    }
}

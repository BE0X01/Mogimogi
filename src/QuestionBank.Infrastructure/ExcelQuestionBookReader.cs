using ClosedXML.Excel;
using QuestionBank.Core.Abstractions;
using QuestionBank.Core.Models;

namespace QuestionBank.Infrastructure;

public sealed class ExcelQuestionBookReader : IQuestionBookReader
{
    private static readonly string[] Headers = ["문제", "선지1", "선지2", "선지3", "선지4", "정답ID"];

    public QuestionBook Read(string filePath)
    {
        // Excel에서 파일을 열어 놓은 경우에도 읽기 공유가 허용되면 가져옵니다.
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new XLWorkbook(stream);
        if (workbook.Worksheets.Count == 0)
        {
            throw new InvalidDataException("워크시트가 없습니다.");
        }
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RowsUsed(XLCellsUsedOptions.Contents).ToArray();
        var questions = new List<Question>();
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var headerChecked = false;
        foreach (var row in rows)
        {
            var cells = Enumerable.Range(1, 7).Select(row.Cell).ToArray();
            if (cells.All(c => c.IsEmpty()))
            {
                continue;
            }
            if (cells.Any(c => c.HasFormula))
            {
                errors.Add($"{row.RowNumber()}행: 수식 대신 텍스트 값을 입력하세요.");
                continue;
            }
            var values = cells.Select(c => c.GetString().Trim()).ToArray();
            if (!headerChecked)
            {
                headerChecked = true;
                if (Headers.Select((h, i) => values[i].Equals(h, StringComparison.OrdinalIgnoreCase)).All(x => x))
                {
                    if (values[6].Length > 0 && !values[6].Equals("QuestionID", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"{row.RowNumber()}행: 7열 제목은 QuestionID로 입력하세요.");
                    }
                    continue;
                }
            }
            try
            {
                if (cells.Any(c => c.DataType == XLDataType.Error))
                {
                    throw new ArgumentException("오류가 있는 셀이 있습니다.");
                }
                var options = Enumerable.Range(1, 4).Select(i => new AnswerOption($"AS{i}", values[i]));
                var question = new Question(values[0], options, values[5], values[6]);
                if (!ids.Add(question.Id))
                {
                    throw new ArgumentException("중복 문제 또는 중복 QuestionID입니다. 별도 문제라면 고유 ID를 지정하세요.");
                }
                questions.Add(question);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"{row.RowNumber()}행: {ex.Message}");
            }
        }
        if (errors.Count > 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, errors.Take(20)) +
                (errors.Count > 20 ? $"\n외 {errors.Count - 20}건" : ""));
        }
        if (questions.Count == 0)
        {
            throw new InvalidDataException("첫 번째 시트에 문제가 없습니다.");
        }
        var name = Path.GetFileNameWithoutExtension(filePath);
        var separator = name.IndexOf('_');
        var category = separator > 0 ? name[..separator] : "미분류";
        return new QuestionBook(Path.GetFileName(filePath), category, name, filePath, questions.AsReadOnly());
    }
}

using Mogimogi.Core.Services;

namespace Mogimogi.WinForms.Forms;

public sealed class ResultForm : Form
{
    public ResultForm(QuizSession session)
    {
        Ui.Configure(this, "Mogimogi · 풀이 결과", new Size(880, 740));
        var answers = session.Answers;
        var correct = answers.Count(a => a.IsCorrect);
        var wrong = answers.Count - correct;
        var root = Ui.Stack(4);
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(Ui.Label("이번 풀이 결과", 22, true), 0, 0);
        var rate = answers.Count == 0 ? 0 : (double)correct / answers.Count;
        root.Controls.Add(Ui.Label($"제출 {answers.Count} / 출제 {session.Total}   ·   정답 {correct}   ·   오답 {wrong}   ·   정답률 {rate:P0}", 12), 0, 1);
        var review = string.Join("\r\n\r\n", answers.Select((a, i) =>
        {
            var selected = a.DisplayOptions.Single(o => o.Id == a.SelectedAnswerId);
            var answer = a.DisplayOptions.Single(o => o.Id == a.CorrectAnswerId);
            return $"{i + 1}. [{(a.IsCorrect ? "정답" : "오답")}] {a.QuestionText}\r\n" +
                $"내 답: {selected.Text}\r\n정답: {answer.Text}";
        }));
        root.Controls.Add(Ui.ReadOnlyText(review), 0, 2);
        var close = Ui.Button("문제집 선택으로", true);
        close.Margin = new Padding(0, 16, 0, 0);
        close.Click += (_, _) => Close();
        root.Controls.Add(close, 0, 3);
        Controls.Add(root);
    }
}

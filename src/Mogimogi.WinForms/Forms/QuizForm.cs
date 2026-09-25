using Mogimogi.Core.Services;

namespace Mogimogi.WinForms.Forms;

public sealed class QuizForm : Form
{
    private readonly QuizSession _session;
    private readonly Label _progress = Ui.Label("", 15, true);
    private readonly TextBox _question = Ui.ReadOnlyText();
    private readonly TextBox _feedback = Ui.ReadOnlyText("선지 하나를 선택하고 답안을 제출하세요.");
    private readonly RadioButton[] _options = new RadioButton[4];
    private readonly Button _action = Ui.Button("답안 제출", true);
    private bool _saving;
    private bool _finished;

    public QuizForm(QuizSession session)
    {
        _session = session;
        Ui.Configure(this, "Mogimogi · 문제 풀이", new Size(880, 820));
        MinimumSize = new Size(760, 730);
        var root = Ui.Stack(5);
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(_progress, 0, 0);
        _question.Font = new Font("Malgun Gothic", 14F);
        _question.Margin = new Padding(0, 4, 0, 16);
        root.Controls.Add(_question, 0, 1);
        // RadioButton은 같은 부모에 두어 단일 선택을 보장합니다.
        var choices = new TableLayoutPanel { ColumnCount = 2, RowCount = 4, Dock = DockStyle.Fill };
        choices.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++)
        {
            choices.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            _options[i] = new RadioButton
            {
                Text = (i + 1).ToString(), Dock = DockStyle.Fill, AutoSize = true,
                AccessibleName = $"선지 {i + 1}", UseMnemonic = false
            };
            choices.Controls.Add(_options[i], 0, i);
            var text = Ui.ReadOnlyText();
            text.Name = $"ChoiceText{i}";
            text.Font = new Font("Malgun Gothic", 12F);
            text.Margin = new Padding(0, 4, 0, 4);
            var index = i;
            text.Click += (_, _) =>
            {
                if (_options[index].Enabled)
                {
                    _options[index].Checked = true;
                }
            };
            choices.Controls.Add(text, 1, i);
        }
        root.Controls.Add(choices, 0, 2);
        _feedback.Margin = new Padding(0, 12, 0, 12);
        root.Controls.Add(_feedback, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var quit = Ui.Button("풀이 종료");
        quit.Click += (_, _) => Close();
        actions.Controls.AddRange([_action, quit]);
        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);
        _action.Click += async (_, _) => await SubmitOrNextAsync();
        FormClosing += ConfirmClose;
        RenderQuestion();
    }

    private void RenderQuestion()
    {
        var current = _session.Current;
        _progress.Text = $"{_session.Index + 1} / {_session.Total}   ·   {current.BookName}";
        _question.Text = current.Question.Text.Replace("\n", "\r\n");
        for (var i = 0; i < 4; i++)
        {
            _options[i].Checked = false;
            _options[i].Enabled = true;
            _options[i].Tag = current.DisplayOptions[i].Id;
            _options[i].AccessibleDescription = current.DisplayOptions[i].Text;
            var text = (TextBox)Controls.Find($"ChoiceText{i}", true).Single();
            text.Text = current.DisplayOptions[i].Text.Replace("\n", "\r\n");
            text.BackColor = Color.White;
        }
        _feedback.Text = "선지 하나를 선택하고 답안을 제출하세요.";
        _feedback.ForeColor = Ui.Ink;
        _action.Text = "답안 제출";
        _action.Enabled = true;
        // 자동 포커스로 첫 선지가 선택되는 것을 피합니다.
        ActiveControl = _action;
    }

    private async Task SubmitOrNextAsync()
    {
        if (_saving)
        {
            return;
        }
        if (_session.CurrentAnswered)
        {
            _session.MoveNext();
            if (_session.IsCompleted)
            {
                _finished = true;
                Close();
                return;
            }
            RenderQuestion();
            return;
        }
        var selected = _options.FirstOrDefault(o => o.Checked)?.Tag as string;
        if (selected is null)
        {
            _feedback.Text = "선지를 하나 선택해주세요.";
            return;
        }
        _saving = true;
        _action.Enabled = false;
        foreach (var option in _options)
        {
            option.Enabled = false;
        }
        try
        {
            var answer = await _session.SubmitAsync(selected);
            var correctIndex = Array.FindIndex(answer.DisplayOptions, o => o.Id == answer.CorrectAnswerId);
            var correct = answer.DisplayOptions[correctIndex];
            _feedback.Text = (answer.IsCorrect ? "정답입니다!" : "오답입니다.") +
                $"\r\n정답: {correctIndex + 1}번 · {correct.Text}\r\n풀이 기록을 저장했습니다.";
            _feedback.ForeColor = answer.IsCorrect ? Color.DarkGreen : Color.Firebrick;
            ((TextBox)Controls.Find($"ChoiceText{correctIndex}", true).Single()).BackColor = Color.FromArgb(232, 246, 237);
            _action.Text = _session.Index + 1 == _session.Total ? "결과 보기" : "다음 문제";
        }
        catch (Exception ex)
        {
            _feedback.Text = "기록을 저장하지 못했습니다. 문제를 넘기지 않고 대기합니다.\r\n" + ex.Message;
            _feedback.ForeColor = Color.Firebrick;
            _action.Text = "저장 재시도";
        }
        finally
        {
            _saving = false;
            _action.Enabled = true;
        }
    }

    private void ConfirmClose(object? sender, FormClosingEventArgs e)
    {
        if (_finished)
        {
            return;
        }
        if (_saving)
        {
            e.Cancel = true;
            return;
        }
        var text = _session.HasPendingSave
            ? "현재 답안은 아직 저장되지 않았습니다. 종료하면 저장되지 않은 답안은 사라집니다. 종료할까요?"
            : "풀이를 종료할까요? 제출한 답안은 저장되어 있으며, 풀지 않은 문제는 기록하지 않습니다.";
        e.Cancel = MessageBox.Show(this, text, "풀이 종료", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes;
    }
}

using System.Diagnostics;
using Mogimogi.Core.Abstractions;
using Mogimogi.Core.Models;
using Mogimogi.Core.Services;
using Mogimogi.Infrastructure;

namespace Mogimogi.WinForms.Forms;

public sealed class MainForm : Form
{
    private readonly string _dataDirectory;
    private readonly string _userDirectory;
    private readonly QuestionBookCatalog _catalog;
    private readonly IHistoryStore _history;
    private readonly ComboBox _categories = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ListBox _books = new() { Dock = DockStyle.Fill, IntegralHeight = false, DisplayMember = nameof(QuestionBook.Name) };
    private readonly NumericUpDown _count = new() { Minimum = 1, Maximum = 1, Value = 1, Width = 110 };
    private readonly Label _total = Ui.Label("문제집을 선택하세요.");
    private readonly Label _status = Ui.Label("문제집을 불러오는 중입니다.");
    private readonly Button _start = Ui.Button("풀이 시작", true);
    private readonly Button _refresh = Ui.Button("새로고침");
    private readonly Button _errors = Ui.Button("불러오기 오류");
    private IReadOnlyList<QuestionBook> _loadedBooks = [];
    private IReadOnlyList<string> _loadErrors = [];
    private bool _loading;

    public MainForm(string dataDirectory, string userDirectory, QuestionBookCatalog catalog, IHistoryStore history)
    {
        _dataDirectory = dataDirectory;
        _userDirectory = userDirectory;
        _catalog = catalog;
        _history = history;
        Ui.Configure(this, "Mogimogi · 문제집 선택", new Size(820, 750));
        var root = Ui.Stack(9);
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var i = 5; i < 9; i++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        root.Controls.Add(Ui.Label("오늘 풀 문제를 골라보세요", 21, true), 0, 0);
        root.Controls.Add(Ui.Label("Data 폴더의 엑셀 문제집을 불러와 무작위로 출제합니다."), 0, 1);
        root.Controls.Add(Ui.Label("분류"), 0, 2);
        root.Controls.Add(_categories, 0, 3);
        root.Controls.Add(_books, 0, 4);
        root.Controls.Add(_total, 0, 5);
        var settings = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 16) };
        settings.Controls.Add(Ui.Label("풀 문제 수"));
        settings.Controls.Add(_count);
        var all = Ui.Button("전체 문제");
        all.Click += (_, _) => _count.Value = _count.Maximum;
        settings.Controls.Add(all);
        settings.Controls.Add(_start);
        root.Controls.Add(settings, 0, 6);
        var tools = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var dataFolder = Ui.Button("Data 폴더 열기");
        var recordsFolder = Ui.Button("기록 폴더 열기");
        tools.Controls.AddRange([_refresh, dataFolder, recordsFolder, _errors]);
        root.Controls.Add(tools, 0, 7);
        root.Controls.Add(_status, 0, 8);
        Controls.Add(root);
        _errors.Visible = false;
        _start.Enabled = false;
        _categories.SelectedIndexChanged += (_, _) => FilterBooks();
        _books.SelectedIndexChanged += (_, _) => UpdateCount();
        _refresh.Click += async (_, _) => await ReloadAsync();
        _start.Click += async (_, _) => await StartQuizAsync();
        dataFolder.Click += (_, _) => OpenFolder(_dataDirectory);
        recordsFolder.Click += (_, _) => OpenFolder(_userDirectory);
        _errors.Click += (_, _) => ShowErrors();
        Shown += async (_, _) => await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_loading)
        {
            return;
        }
        _loading = true;
        _refresh.Enabled = _start.Enabled = _categories.Enabled = _books.Enabled = false;
        _status.Text = "문제집을 불러오는 중입니다…";
        var previousCategory = _categories.SelectedItem as string;
        try
        {
            var result = await Task.Run(() => _catalog.Load(_dataDirectory));
            if (IsDisposed)
            {
                return;
            }
            _loadedBooks = result.Books;
            _loadErrors = result.Errors;
            _categories.Items.Clear();
            _categories.Items.Add("전체");
            _categories.Items.AddRange(_loadedBooks.Select(b => b.Category).Distinct().Order().Cast<object>().ToArray());
            _categories.SelectedItem = previousCategory is not null && _categories.Items.Contains(previousCategory)
                ? previousCategory : "전체";
            _errors.Visible = _loadErrors.Count > 0;
            _errors.Text = $"불러오기 오류 ({_loadErrors.Count})";
            _status.Text = _loadedBooks.Count == 0
                ? "사용할 문제집이 없습니다. Data 폴더에 .xlsx 파일을 넣고 새로고침하세요."
                : $"문제집 {_loadedBooks.Count}개 · 오류 {_loadErrors.Count}건";
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                _loadedBooks = [];
                _books.Items.Clear();
                _status.Text = "문제집을 불러오지 못했습니다.";
                MessageBox.Show(this, ex.Message, "불러오기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            _loading = false;
            if (!IsDisposed)
            {
                _refresh.Enabled = _categories.Enabled = _books.Enabled = true;
                UpdateCount();
            }
        }
    }

    private void FilterBooks()
    {
        var selected = _categories.SelectedItem as string;
        _books.Items.Clear();
        foreach (var book in _loadedBooks.Where(b => selected == "전체" || b.Category == selected))
        {
            _books.Items.Add(book);
        }
        if (_books.Items.Count > 0)
        {
            _books.SelectedIndex = 0;
        }
        UpdateCount();
    }

    private void UpdateCount()
    {
        var book = _books.SelectedItem as QuestionBook;
        _total.Text = book is null ? "문제집을 선택하세요." : $"{book.Name} · 전체 {book.Questions.Count}문제";
        _count.Maximum = Math.Max(1, book?.Questions.Count ?? 1);
        _count.Enabled = book is not null;
        _start.Enabled = !_loading && book is not null;
    }

    private async Task StartQuizAsync()
    {
        if (_books.SelectedItem is not QuestionBook book)
        {
            return;
        }
        _start.Enabled = _refresh.Enabled = false;
        try
        {
            // 손상된 기록을 덮어쓰지 않도록 시작 전에 읽기·형식 검증을 합니다.
            await _history.ReadAsync();
            if (IsDisposed)
            {
                return;
            }
            var session = new QuizService(_history).CreateSession([book], (int)_count.Value);
            using var quiz = new QuizForm(session);
            quiz.ShowDialog(this);
            if (session.Answers.Count > 0)
            {
                using var result = new ResultForm(session);
                result.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                MessageBox.Show(this, ex.Message, "풀이 시작 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _refresh.Enabled = true;
                UpdateCount();
            }
        }
    }

    private void ShowErrors()
    {
        using var dialog = new Form();
        Ui.Configure(dialog, "엑셀 불러오기 오류", new Size(820, 650));
        dialog.Controls.Add(Ui.ReadOnlyText(string.Join("\r\n\r\n", _loadErrors)));
        dialog.ShowDialog(this);
    }

    private void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "폴더 열기 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

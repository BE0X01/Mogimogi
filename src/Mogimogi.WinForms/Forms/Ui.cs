namespace Mogimogi.WinForms.Forms;

internal static class Ui
{
    internal static readonly Color Ink = Color.FromArgb(28, 38, 57);
    internal static readonly Color Accent = Color.FromArgb(61, 92, 201);

    internal static void Configure(Form form, string title, Size size)
    {
        form.Text = title;
        form.Font = new Font("Malgun Gothic", 10F);
        form.ForeColor = Ink;
        form.BackColor = Color.FromArgb(246, 248, 252);
        form.StartPosition = FormStartPosition.CenterScreen;
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.ClientSize = size;
        form.MinimumSize = new Size(720, 620);
        form.Padding = new Padding(24);
    }

    internal static Label Label(string text, float size = 10, bool bold = false)
    {
        return new Label
        {
            Text = text, AutoSize = true, Dock = DockStyle.Fill,
            Font = new Font("Malgun Gothic", size, bold ? FontStyle.Bold : FontStyle.Regular),
            Padding = new Padding(0, 5, 0, 8), UseMnemonic = false
        };
    }

    internal static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, MinimumSize = new Size(120, 42),
            Padding = new Padding(12, 5, 12, 5), FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : Ink,
            Margin = new Padding(0, 0, 10, 0), UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? Accent : Color.FromArgb(214, 222, 235);
        return button;
    }

    internal static TextBox ReadOnlyText(string text = "")
    {
        return new TextBox
        {
            Text = text, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill, BackColor = Color.White, ForeColor = Ink,
            BorderStyle = BorderStyle.FixedSingle, TabStop = false
        };
    }

    internal static TableLayoutPanel Stack(int rows)
    {
        return new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows,
            Margin = Padding.Empty, Padding = Padding.Empty
        };
    }
}

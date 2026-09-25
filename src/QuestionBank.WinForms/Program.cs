using QuestionBank.Infrastructure;
using QuestionBank.WinForms.Forms;

namespace QuestionBank.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        var userDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuestionBank");
        var history = new JsonHistoryStore(Path.Combine(userDirectory, "history.json"));
        Application.Run(new MainForm(dataDirectory, userDirectory,
            new QuestionBookCatalog(new ExcelQuestionBookReader()), history));
    }
}

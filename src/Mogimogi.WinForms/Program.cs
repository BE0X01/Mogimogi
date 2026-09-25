using Mogimogi.Infrastructure;
using Mogimogi.WinForms.Forms;

namespace Mogimogi.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        string userDirectory;
        try
        {
            userDirectory = UserDataDirectory.Prepare(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"풀이 기록 폴더를 준비하지 못했습니다.\n{ex.Message}",
                "Mogimogi · 시작 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        var history = new JsonHistoryStore(Path.Combine(userDirectory, "history.json"));
        Application.Run(new MainForm(dataDirectory, userDirectory,
            new QuestionBookCatalog(new ExcelQuestionBookReader()), history));
    }
}

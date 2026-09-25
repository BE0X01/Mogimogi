namespace Mogimogi.Infrastructure;

public static class UserDataDirectory
{
    public static string Prepare(string localApplicationData)
    {
        var directory = Path.Combine(localApplicationData, "Mogimogi");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, "history.json");
        // 최초 MVP의 저장 경로입니다. 기존 사용자 기록을 가져오기 위해 유지합니다.
        var legacy = Path.Combine(localApplicationData, "QuestionBank", "history.json");
        if (File.Exists(destination) || !File.Exists(legacy))
        {
            return directory;
        }

        // 이전 버전이나 다른 인스턴스가 기록을 쓰는 동안에는 이관하지 않습니다.
        using var destinationLock = new FileStream(destination + ".lock", FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        using var legacyLock = new FileStream(legacy + ".lock", FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(destination))
        {
            return directory;
        }
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.Copy(legacy, temporary);
            if (File.Exists(legacy + ".bak") && !File.Exists(destination + ".bak"))
            {
                File.Copy(legacy + ".bak", destination + ".bak");
            }
            File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
        // 원본은 삭제하지 않습니다. 새 기록이 있으면 다음 실행에서 재이관하지 않습니다.
        return directory;
    }
}

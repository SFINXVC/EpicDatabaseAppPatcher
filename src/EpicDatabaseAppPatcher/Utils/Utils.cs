namespace EpicDatabaseAppPatcher.Utils;

public static class Utils {
    public static string GetExecutableFileName() {
        string[] cmd = Environment.GetCommandLineArgs();
        if (cmd.Length > 0 && !string.IsNullOrEmpty(cmd[0]))
            return Path.GetFileName(cmd[0]);

        try {
            string? proc = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(proc))
                return Path.GetFileName(proc);
        } catch {
            // ignore
        }

        return "EpicDatabaseAppPatcher";
    }
}
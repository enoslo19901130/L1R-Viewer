namespace L1R.Shell;

/// <summary>
/// Finds built MapViewer / PakBrowser / CLI binaries relative to Shell or repo root.
/// </summary>
public static class ToolLocator
{
    public static string? FindMapViewer()
        => Find("L1R.MapViewer", "L1MapViewerCore.exe");

    public static string? FindPakBrowser()
        => Find("L1R.PakBrowser", "PakViewer.exe");

    public static string? FindCli()
        => Find("L1R.Cli", "pakviewer-cli.exe");

    public static string? FindGettingStarted()
    {
        foreach (var root in CandidateRoots())
        {
            string p = Path.Combine(root, "docs", "GETTING-STARTED.md");
            if (File.Exists(p)) return p;
        }
        return null;
    }

    static string? Find(string project, string exe)
    {
        foreach (var root in CandidateRoots())
        {
            string[] cands =
            {
                Path.Combine(root, "src", project, "bin", "Release", "net10.0-windows", exe),
                Path.Combine(root, "src", project, "bin", "Release", "net10.0", exe),
                Path.Combine(root, "src", project, "bin", "Debug", "net10.0-windows", exe),
                Path.Combine(root, "src", project, "bin", "Debug", "net10.0", exe),
                // same directory as shell (publish layout)
                Path.Combine(root, exe),
            };
            foreach (var c in cands)
            {
                if (File.Exists(c)) return c;
            }
        }
        return null;
    }

    static IEnumerable<string> CandidateRoots()
    {
        // Shell bin: .../src/L1R.Shell/bin/Release/net10.0-windows
        string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        yield return baseDir;

        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            yield return dir.FullName;
            // detect repo root by L1R-Viewer.slnx
            if (File.Exists(Path.Combine(dir.FullName, "L1R-Viewer.slnx")))
                yield break;
            dir = dir.Parent;
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// After a player build, copies CREDITS.txt (repo root) next to the built game, so the credits the sounds' licences
/// require always ship with the game folder. Does nothing if the file or the build folder is missing.
/// </summary>
public sealed class CopyCreditsToBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 100;

    public void OnPostprocessBuild(BuildReport report)
    {
        string source = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "CREDITS.txt");
        string folder = Path.GetDirectoryName(report.summary.outputPath);
        if (!File.Exists(source) || string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
        File.Copy(source, Path.Combine(folder, "CREDITS.txt"), true);
    }
}

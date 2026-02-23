#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class IBayeBuildPipeline
{
    public static void BuildWindowsPlayer()
    {
        string output = Environment.GetEnvironmentVariable("IBAYE_BUILD_OUTPUT");
        if (string.IsNullOrWhiteSpace(output))
        {
            output = Path.GetFullPath(Path.Combine("Builds", "Windows", "iBayeUnity.exe"));
        }

        output = Path.GetFullPath(output);
        string outputDir = Path.GetDirectoryName(output);
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            throw new InvalidOperationException("Invalid build output path: " + output);
        }

        Directory.CreateDirectory(outputDir);
        EnsureDefaultSceneExistsAndRegistered();

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        Debug.Log("Building Windows player to: " + output);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Build failed: " + report.summary.result +
                " | errors=" + report.summary.totalErrors +
                " | warnings=" + report.summary.totalWarnings
            );
        }

        Debug.Log(
            "Build succeeded. Size=" + report.summary.totalSize +
            " bytes | output=" + output
        );
    }

    private static void EnsureDefaultSceneExistsAndRegistered()
    {
        const string defaultScene = "Assets/Scenes/Main.unity";
        if (!File.Exists(defaultScene))
        {
            IBayeSceneBuilder.CreateDefaultShellScene();
        }

        bool exists = EditorBuildSettings.scenes.Any(s => string.Equals(s.path, defaultScene, StringComparison.Ordinal));
        if (!exists)
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(defaultScene, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif

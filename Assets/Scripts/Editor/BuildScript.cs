using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildScript
{
    private static readonly string[] ClientScenes = {
        "Assets/Scenes/LoginScene.unity",
        "Assets/Scenes/ShipEditor.unity",
        "Assets/Scenes/TestMultiplayerScene.unity"
    };

    private static readonly string[] ServerScenes = {
        "Assets/Scenes/TestMultiplayerScene.unity"
    };


    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildWindowsServer();
        BuildLinuxServer();
        BuildMacServer();
        BuildWindowsClient();
        BuildLinuxClient();
        BuildMacClient();
    }


    [MenuItem("Build/Build Server/Windows")]
    public static void BuildWindowsServer() => BuildServer(BuildTarget.StandaloneWindows64, "Builds/Windows/Server/Server.exe");

    [MenuItem("Build/Build Server/Linux")]
    public static void BuildLinuxServer() => BuildServer(BuildTarget.StandaloneLinux64, "Builds/Linux/Server/Server.x86_64");

    [MenuItem("Build/Build Server/macOS")]
    public static void BuildMacServer() => BuildServer(BuildTarget.StandaloneOSX, "Builds/Mac/Server/Server.app");


    [MenuItem("Build/Build & Run Server/Windows")]
    public static void BuildAndRunWindowsServer() => BuildServer(BuildTarget.StandaloneWindows64, "Builds/Windows/Server/Server.exe", true);

    [MenuItem("Build/Build & Run Server/Linux")]
    public static void BuildAndRunLinuxServer() => BuildServer(BuildTarget.StandaloneLinux64, "Builds/Linux/Server/Server.x86_64", true);

    [MenuItem("Build/Build & Run Server/macOS")]
    public static void BuildAndRunMacServer() => BuildServer(BuildTarget.StandaloneOSX, "Builds/Mac/Server/Server.app", true);


    [MenuItem("Build/Build Client/Windows")]
    public static void BuildWindowsClient() => BuildClient(BuildTarget.StandaloneWindows64, "Builds/Windows/Client/Client.exe", false);

    [MenuItem("Build/Build & Run Client/Windows")]
    public static void BuildAndRunWindowsClient() => BuildClient(BuildTarget.StandaloneWindows64, "Builds/Windows/Client/Client.exe", true);

    [MenuItem("Build/Build Client/Linux")]
    public static void BuildLinuxClient() => BuildClient(BuildTarget.StandaloneLinux64, "Builds/Linux/Client/Client.x86_64", false);

    [MenuItem("Build/Build Client/macOS")]
    public static void BuildMacClient() => BuildClient(BuildTarget.StandaloneOSX, "Builds/Mac/Client/Client.app", false);


    private static void BuildServer(BuildTarget target, string path, bool runAfter = false)
    {
        Debug.Log($"Building Server for {target}...");

        var prevSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = ServerScenes,
            locationPathName = path,
            target = target,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildPipeline.BuildPlayer(options);

        EditorUserBuildSettings.standaloneBuildSubtarget = prevSubtarget;

        Debug.Log($"Built Server for {target} at {path}");

        if (runAfter)
        {
            string serverArgs = "-batchmode -nographics -port 7777 -ip auto -logFile -";
            RunBuiltPlayer(path, serverArgs);
        }
    }

    private static void BuildClient(BuildTarget target, string path, bool runAfter)
    {
        Debug.Log($"Building Client for {target}...");

        var prevSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = ClientScenes,
            locationPathName = path,
            target = target,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildPipeline.BuildPlayer(options);

        EditorUserBuildSettings.standaloneBuildSubtarget = prevSubtarget;

        Debug.Log($"Built Client for {target} at {path}");

        if (runAfter)
        {
            RunBuiltPlayer(path, "");
        }
    }

    private static void RunBuiltPlayer(string path, string args)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);

            if (Directory.Exists(fullPath) && fullPath.EndsWith(".app"))
            {
                string appName = Path.GetFileNameWithoutExtension(fullPath);
                fullPath = Path.Combine(fullPath, "Contents", "MacOS", appName);
            }

            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                Debug.LogError($"File not found: {fullPath}");
                return;
            }

            Debug.Log($"Running: {fullPath} with args: {args}");

#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k \"\"{fullPath}\" {args}\"",
                WorkingDirectory = Path.GetDirectoryName(fullPath),
                UseShellExecute = true
            });
#elif UNITY_EDITOR_LINUX
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "gnome-terminal",
                Arguments = $"-- bash -c \"'{fullPath}' {args}; read -p 'Press Enter to close...'\"",
                WorkingDirectory = Path.GetDirectoryName(fullPath),
                UseShellExecute = false
            });
#elif UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", $"-a Terminal \"{fullPath}\" --args {args}");
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to run built player: {ex.Message}");
        }
    }
}
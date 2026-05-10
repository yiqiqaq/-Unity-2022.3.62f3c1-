using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 自动将所需场景添加到 Build Settings。
/// 菜单: Tools → Configure Build Scenes
/// 也会在项目打开时自动执行一次。
/// </summary>
public static class BuildSettingsConfigurator
{
    private static readonly string[] RequiredScenes =
    {
        "Assets/Scenes/BaseScene.unity",
        "Assets/Scenes/StartupScene.unity",
        "Assets/Scenes/LoginScene.unity",
        "Assets/Scenes/RegisterScene.unity",
        "Assets/Scenes/IntroScene.unity",
        "Assets/Scenes/Chapter1Scene.unity"
    };

    [MenuItem("Tools/Configure Build Scenes")]
    public static void Configure()
    {
        var existing = EditorBuildSettings.scenes;
        var existingPaths = new HashSet<string>();
        foreach (var s in existing)
            existingPaths.Add(s.path);

        var list = new List<EditorBuildSettingsScene>();
        foreach (var path in RequiredScenes)
        {
            if (existingPaths.Contains(path))
            {
                foreach (var s in existing)
                {
                    if (s.path == path) { list.Add(s); break; }
                }
            }
            else
            {
                list.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log("[BuildSettings] Added: " + path);
            }
        }

        EditorBuildSettings.scenes = list.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildSettings] Configured " + list.Count + " scenes.");
    }

    [InitializeOnLoadMethod]
    private static void AutoConfigure()
    {
        var existingPaths = new HashSet<string>();
        foreach (var s in EditorBuildSettings.scenes)
            existingPaths.Add(s.path);

        bool missing = false;
        foreach (var p in RequiredScenes)
        {
            if (!existingPaths.Contains(p)) { missing = true; break; }
        }

        if (missing)
        {
            Debug.Log("[BuildSettings] Missing scenes detected, auto-configuring...");
            Configure();
        }
    }
}

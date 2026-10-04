// =====================================================================
//  BuildTools.cs  —  One-click builds
//  Editor-only. Menu: Tools ▸ AR Survival ▸ Build iOS (Xcode project)
//                     Tools ▸ AR Survival ▸ Build Android APK
//
//  iOS: Unity exports an Xcode project to <project>/Builds/iOS. Open
//       Builds/iOS/Unity-iPhone.xcodeproj in Xcode, choose your Team
//       (Personal Team is fine) and press Run with the iPhone connected.
//  Builds/ is git-ignored, so build output never ends up in the repo.
// =====================================================================
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    public static class BuildTools
    {
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        [MenuItem("Tools/AR Survival/Build iOS (Xcode project)")]
        public static void BuildIOS()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[BuildTools] iOS Build Support is not installed.");
                return;
            }

            PlayerSettings.iOS.appleEnableAutomaticSigning = true;   // pick the Team in Xcode
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            string path = Path.Combine(ProjectRoot, "Builds", "iOS");
            Build(BuildTarget.iOS, path);
        }

        [MenuItem("Tools/AR Survival/Build Android APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[BuildTools] Android Build Support is not installed.");
                return;
            }
            EditorUserBuildSettings.buildAppBundle = false;
            string path = Path.Combine(ProjectRoot, "Builds", "Android", "ARSurvivalShooter.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Build(BuildTarget.Android, path);
        }

        private static void Build(BuildTarget target, string location)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildTools] No scenes in Build Settings.");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"<color=#00E5FF>[BuildTools]</color> {target} build SUCCEEDED → {location} " +
                          $"({s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalSeconds:0}s)");
            else
                Debug.LogError($"[BuildTools] {target} build {s.result} with {s.totalErrors} error(s). See the Console.");
        }
    }
}
#endif

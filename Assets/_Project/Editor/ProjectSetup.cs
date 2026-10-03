// =====================================================================
//  ProjectSetup.cs  —  AR Survival Shooter (Phase 1 automation)
//  Editor-only tool. Menu: Tools ▸ AR Survival ▸ Run Phase 1 Setup
//
//  What it does (idempotent — safe to run more than once):
//   1. Creates the Assets/_Project folder structure
//   2. Applies Player Settings (iOS + Android): company, product,
//      bundle id, camera usage text, min iOS 15, portrait, IL2CPP/ARM64
//   3. Builds the clean Game scene: AR Session + XR Origin (Mobile AR)
//      with AR Plane Manager (Horizontal), AR Raycast Manager and
//      AR Anchor Manager, plus a temporary default plane prefab
//   4. Puts Game.unity as the only scene in the build list
//   5. Sets import settings on the name texture (Repeat wrap, sRGB, alpha)
// =====================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARSurvival.EditorTools
{
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Game.unity";
        private const string PlanePrefabPath = Root + "/Prefabs/TempDefaultPlane.prefab";
        private const string NameTexturePath = Root + "/Textures/name_plane.png";
        private const string BundleId = "com.brimstudios.arsurvivalshooter";

        [MenuItem("Tools/AR Survival/Run Phase 1 Setup")]
        public static void RunAll()
        {
            CreateFolders();
            ConfigurePlayerSettings();
            ConfigureNameTexture();
            CreateGameScene();
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00E5FF>[ProjectSetup]</color> Phase 1 setup complete.");
        }

        // ---------------------------------------------------------------
        // 1. Folders
        // ---------------------------------------------------------------
        private static void CreateFolders()
        {
            EnsureFolder("Assets", "_Project");
            string[] top = { "Scenes", "Scripts", "Prefabs", "Materials", "Textures",
                             "Models", "Audio", "ScriptableObjects", "Editor" };
            foreach (var f in top) EnsureFolder(Root, f);

            string[] scripts = { "AR", "Core", "Player", "Combat", "Enemies", "UI", "Data", "Audio" };
            foreach (var f in scripts) EnsureFolder(Root + "/Scripts", f);
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        // ---------------------------------------------------------------
        // 2. Player Settings
        // ---------------------------------------------------------------
        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Brim Studios";
            PlayerSettings.productName = "AR Survival Shooter";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // iOS
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.cameraUsageDescription = "Camera is used for augmented reality gameplay.";
            PlayerSettings.iOS.targetOSVersionString = "15.0";

            // Android (backup build path)
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }

        // ---------------------------------------------------------------
        // 3. Name texture import settings (tiles across the AR plane)
        // ---------------------------------------------------------------
        private static void ConfigureNameTexture()
        {
            var importer = AssetImporter.GetAtPath(NameTexturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[ProjectSetup] name_plane.png not found at " + NameTexturePath + " — skipped.");
                return;
            }
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------
        // 4. Game scene
        // ---------------------------------------------------------------
        private static void CreateGameScene()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                Debug.Log("[ProjectSetup] Game.unity already exists — opening it instead of rebuilding.");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                SetBuildScenes();
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // XR Origin brings its own AR camera, so remove the default one.
            GameObject defaultCam = GameObject.Find("Main Camera");
            if (defaultCam != null) Object.DestroyImmediate(defaultCam);

            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Session");
            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)");

            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            if (origin == null)
            {
                Debug.LogError("[ProjectSetup] Could not create XR Origin (Mobile AR). " +
                               "Add it manually: Hierarchy ▸ right-click ▸ XR ▸ XR Origin (Mobile AR).");
            }
            else
            {
                GameObject go = origin.gameObject;

                ARPlaneManager planes = go.GetComponent<ARPlaneManager>();
                if (planes == null) planes = go.AddComponent<ARPlaneManager>();
                planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                planes.planePrefab = GetOrCreateTempPlanePrefab();

                if (go.GetComponent<ARRaycastManager>() == null) go.AddComponent<ARRaycastManager>();
                if (go.GetComponent<ARAnchorManager>() == null) go.AddComponent<ARAnchorManager>();
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
        }

        private static GameObject GetOrCreateTempPlanePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            if (existing != null) return existing;

            Selection.activeObject = null;
            EditorApplication.ExecuteMenuItem("GameObject/XR/AR Default Plane");
            ARPlane plane = Object.FindAnyObjectByType<ARPlane>();
            if (plane == null)
            {
                Debug.LogWarning("[ProjectSetup] Could not create AR Default Plane — assign a plane prefab manually.");
                return null;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(plane.gameObject, PlanePrefabPath);
            Object.DestroyImmediate(plane.gameObject);
            return prefab;
        }

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
#endif

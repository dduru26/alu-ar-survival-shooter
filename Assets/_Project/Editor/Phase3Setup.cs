// =====================================================================
//  Phase3Setup.cs  —  AR Survival Shooter (Phase 3 automation)
//  Editor-only. Menu: Tools ▸ AR Survival ▸ Run Phase 3 Setup
//
//  1. Creates Difficulty_Easy and Difficulty_Hard assets
//  2. Adds a "GameManager" object (the temporary debug panel was removed in Phase 6)
//  3. Wires placement/plane references and hands tap control to the
//     state machine (taps only work in the Placement state)
// =====================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ARSurvival.AR;
using ARSurvival.Core;

namespace ARSurvival.EditorTools
{
    public static class Phase3Setup
    {
        private const string SoFolder = "Assets/_Project/ScriptableObjects";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/AR Survival/Run Phase 3 Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase3Setup] Stop Play mode first, then run the setup again.");
                return;
            }

            //                                  name    time  hp   spawn max shooter speed dmg
            var easy = GetOrCreateDifficulty("Easy", 90f, 120, 3.2f, 5, 0.30f, 0.85f, 0.8f);
            var hard = GetOrCreateDifficulty("Hard", 120f, 100, 1.8f, 9, 0.45f, 1.25f, 1.3f);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();

            var placement = Object.FindAnyObjectByType<ARPlacementController>();
            var visibility = Object.FindAnyObjectByType<PlaneVisibilityController>();

            var so = new SerializedObject(gm);
            var arr = so.FindProperty("difficulties");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = easy;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = hard;
            so.FindProperty("defaultDifficultyIndex").intValue = 0;
            so.FindProperty("placement").objectReferenceValue = placement;
            so.FindProperty("planeVisibility").objectReferenceValue = visibility;
            so.ApplyModifiedPropertiesWithoutUndo();

            // The state machine now decides when taps place the arena.
            if (placement != null)
            {
                var pso = new SerializedObject(placement);
                pso.FindProperty("acceptTapsOnStart").boolValue = false;
                pso.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00E5FF>[Phase3Setup]</color> Phase 3 setup complete.");
        }

        private static DifficultySettings GetOrCreateDifficulty(string name, float duration, int hp,
            float interval, int maxAlive, float shooter, float speed, float damage)
        {
            string path = $"{SoFolder}/Difficulty_{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<DifficultySettings>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DifficultySettings>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.EditorConfigure(name, duration, hp, interval, maxAlive, shooter, speed, damage);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
#endif

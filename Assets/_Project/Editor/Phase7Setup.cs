#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ARSurvival.Audio;
using ARSurvival.Player;

namespace ARSurvival.EditorTools
{
    public static class Phase7Setup
    {
        private const string AudioRoot = "Assets/_Project/Audio";
        private const string LibraryPath = "Assets/_Project/ScriptableObjects/SoundLibrary.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        [MenuItem("Tools/AR Survival/Run Phase 7 Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase7Setup] Stop Play mode first, then run the setup again.");
                return;
            }

            ConfigureImports();
            SoundLibrary lib = BuildLibrary();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            AudioManager am = Object.FindAnyObjectByType<AudioManager>();
            if (am == null) am = new GameObject("AudioManager").AddComponent<AudioManager>();
            var so = new SerializedObject(am);
            so.FindProperty("library").objectReferenceValue = lib;
            so.FindProperty("playerShooter").objectReferenceValue = Object.FindAnyObjectByType<PlayerShooter>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00E5FF>[Phase7Setup]</color> Phase 7 setup complete.");
        }

        private static void ConfigureImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;

                bool isAmbience = path.Contains("ambient");
                importer.forceToMono = true;
                importer.loadInBackground = isAmbience;
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                s.loadType = isAmbience ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = isAmbience ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                s.quality = 0.7f;
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
        }

        private static SoundLibrary BuildLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            Set(lib.playerShoot, "ThirdParty/player_shoot", 0.55f, 0.06f);
            Set(lib.playerDeath, "Generated/player_death", 0.9f, 0f);
            Set(lib.enemySpawn, "Generated/enemy_spawn", 0.7f, 0.08f);
            Set(lib.enemyShoot, "ThirdParty/enemy_shoot", 0.8f, 0.08f);
            Set(lib.meleeAttack, "Generated/melee_hit", 1f, 0.08f);
            Set(lib.enemyHit, "Generated/enemy_hit", 0.5f, 0.12f);
            Set(lib.enemyDeath, "Generated/enemy_death", 0.8f, 0.1f);
            Set(lib.victory, "Generated/victory", 0.8f, 0f);
            Set(lib.uiClick, "ThirdParty/ui_click", 0.8f, 0.03f);
            Set(lib.uiStart, "ThirdParty/ui_start", 0.8f, 0f);
            Set(lib.ambientLoop, "Generated/ambient_loop", 1f, 0f);

            EditorUtility.SetDirty(lib);
            return lib;
        }

        private static void Set(SoundLibrary.Sound sound, string file, float volume, float pitch)
        {
            sound.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/{file}.wav");
            if (sound.clip == null) Debug.LogWarning($"[Phase7Setup] Missing clip: {file}.wav");
            sound.volume = volume;
            sound.pitchVariation = pitch;
        }
    }
}
#endif

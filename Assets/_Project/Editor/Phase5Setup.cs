#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using ARSurvival.Enemies;

namespace ARSurvival.EditorTools
{
    public static class Phase5Setup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Game.unity";
        private const string ArenaPath = Root + "/Prefabs/Arena.prefab";
        private const string MeleePath = Root + "/Prefabs/Enemy_Melee.prefab";
        private const string ShooterPath = Root + "/Prefabs/Enemy_Shooter.prefab";

        [MenuItem("Tools/AR Survival/Run Phase 5 Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase5Setup] Stop Play mode first, then run the setup again.");
                return;
            }

            Material meleeBody = Mat("MeleeBody", "Lit", new Color(0.85f, 0.15f, 0.15f));
            Material meleeDark = Mat("MeleeDark", "Lit", new Color(0.12f, 0.1f, 0.1f));
            Material visorGlow = Mat("MeleeVisor", "Unlit", new Color(1f, 0.85f, 0.2f));
            Material shooterBody = Mat("ShooterBody", "Lit", new Color(0.55f, 0.25f, 0.95f));
            Material shooterGlow = Mat("ShooterGlow", "Unlit", new Color(1f, 0.5f, 0.1f));

            MeleeEnemy melee = BuildMelee(meleeBody, meleeDark, visorGlow);
            ShooterEnemy shooter = BuildShooter(shooterBody, meleeDark, shooterGlow);

            RemoveDummyFromArena();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SetupEnemiesObject(melee, shooter);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00E5FF>[Phase5Setup]</color> Phase 5 setup complete.");
        }

        private static Material Mat(string name, string kind, Color color)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/" + kind));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos,
                                       Vector3 scale, Material mat, bool keepCollider = false, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static MeleeEnemy BuildMelee(Material body, Material dark, Material visor)
        {
            var root = new GameObject("Enemy_Melee");
            var rig = new GameObject("Rig").transform;
            rig.SetParent(root.transform, false);

            GameObject torso = Part(PrimitiveType.Capsule, "Body", rig, new Vector3(0, 0.21f, 0),
                                    new Vector3(0.24f, 0.21f, 0.24f), body, keepCollider: true);
            Part(PrimitiveType.Cube, "Visor", rig, new Vector3(0, 0.3f, 0.105f), new Vector3(0.17f, 0.045f, 0.04f), visor);
            Part(PrimitiveType.Sphere, "FistL", rig, new Vector3(-0.15f, 0.18f, 0.07f), Vector3.one * 0.09f, dark);
            Part(PrimitiveType.Sphere, "FistR", rig, new Vector3(0.15f, 0.18f, 0.07f), Vector3.one * 0.09f, dark);
            Part(PrimitiveType.Cube, "HornL", rig, new Vector3(-0.07f, 0.41f, 0f), new Vector3(0.03f, 0.08f, 0.03f), dark, euler: new Vector3(0, 0, 20));
            Part(PrimitiveType.Cube, "HornR", rig, new Vector3(0.07f, 0.41f, 0f), new Vector3(0.03f, 0.08f, 0.03f), dark, euler: new Vector3(0, 0, -20));

            var melee = root.AddComponent<MeleeEnemy>();
            var so = new SerializedObject(melee);
            so.FindProperty("maxHealth").intValue = 2;
            so.FindProperty("moveSpeed").floatValue = 0.55f;
            so.FindProperty("scoreValue").intValue = 10;
            so.FindProperty("attackRange").floatValue = 0.5f;
            so.FindProperty("attackDamage").intValue = 10;
            so.FindProperty("attackCooldown").floatValue = 1.2f;
            so.FindProperty("lungeVisual").objectReferenceValue = rig;
            var flash = so.FindProperty("flashRenderers");
            flash.arraySize = 1;
            flash.GetArrayElementAtIndex(0).objectReferenceValue = torso.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MeleePath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MeleeEnemy>();
        }

        private static ShooterEnemy BuildShooter(Material body, Material dark, Material glow)
        {
            var root = new GameObject("Enemy_Shooter");
            var hover = new GameObject("Hover").transform;
            hover.SetParent(root.transform, false);
            hover.localPosition = new Vector3(0, 0.35f, 0);

            GameObject core = Part(PrimitiveType.Sphere, "Core", hover, Vector3.zero, Vector3.one * 0.24f, body, keepCollider: true);
            Part(PrimitiveType.Cylinder, "Ring", hover, Vector3.zero, new Vector3(0.36f, 0.006f, 0.36f), glow);
            Part(PrimitiveType.Sphere, "Eye", hover, new Vector3(0, 0.03f, 0.105f), Vector3.one * 0.06f, glow);
            Part(PrimitiveType.Cylinder, "Barrel", hover, new Vector3(0, -0.04f, 0.14f), new Vector3(0.045f, 0.07f, 0.045f), dark,
                 euler: new Vector3(90, 0, 0));
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(hover, false);
            muzzle.localPosition = new Vector3(0, -0.04f, 0.23f);

            Part(PrimitiveType.Cylinder, "FloorShadow", root.transform, new Vector3(0, 0.003f, 0),
                 new Vector3(0.22f, 0.001f, 0.22f), dark);

            var shooter = root.AddComponent<ShooterEnemy>();
            var so = new SerializedObject(shooter);
            so.FindProperty("maxHealth").intValue = 4;
            so.FindProperty("moveSpeed").floatValue = 0.4f;
            so.FindProperty("scoreValue").intValue = 25;
            so.FindProperty("shootingDistance").floatValue = 1.6f;
            so.FindProperty("shootRange").floatValue = 2.6f;
            so.FindProperty("fireInterval").floatValue = 1.8f;
            so.FindProperty("bulletDamage").intValue = 8;
            so.FindProperty("muzzle").objectReferenceValue = muzzle;
            so.FindProperty("hoverVisual").objectReferenceValue = hover;
            var flash = so.FindProperty("flashRenderers");
            flash.arraySize = 1;
            flash.GetArrayElementAtIndex(0).objectReferenceValue = core.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ShooterPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ShooterEnemy>();
        }

        private static void RemoveDummyFromArena()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ArenaPath);
            Transform dummy = root.transform.Find("TrainingDummy (temporary)");
            if (dummy != null) Object.DestroyImmediate(dummy.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, ArenaPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void SetupEnemiesObject(MeleeEnemy melee, ShooterEnemy shooter)
        {
            GameObject go = GameObject.Find("Enemies") ?? new GameObject("Enemies");
            var factory = go.GetComponent<EnemyFactory>();
            if (factory == null) factory = go.AddComponent<EnemyFactory>();
            var spawner = go.GetComponent<EnemySpawner>();
            if (spawner == null) spawner = go.AddComponent<EnemySpawner>();

            var fso = new SerializedObject(factory);
            fso.FindProperty("meleePrefab").objectReferenceValue = melee;
            fso.FindProperty("shooterPrefab").objectReferenceValue = shooter;
            fso.FindProperty("prewarmPerType").intValue = 10;
            fso.ApplyModifiedPropertiesWithoutUndo();

            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            var sso = new SerializedObject(spawner);
            sso.FindProperty("factory").objectReferenceValue = factory;
            sso.FindProperty("planeManager").objectReferenceValue = Object.FindAnyObjectByType<ARPlaneManager>();
            sso.FindProperty("player").objectReferenceValue = origin != null && origin.Camera != null ? origin.Camera.transform : null;
            sso.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

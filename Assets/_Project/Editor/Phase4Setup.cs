// =====================================================================
//  Phase4Setup.cs  —  AR Survival Shooter (Phase 4 automation)
//  Editor-only. Menu: Tools ▸ AR Survival ▸ Run Phase 4 Setup
//
//  1. Materials + PlayerBullet / EnemyBullet prefabs (sphere + trail + Projectile)
//  2. "Pools" object with a Player pool (30) and an Enemy pool (40)
//  3. AR camera becomes the player: PlayerHealth, PlayerShooter, hitbox
//  4. FeedbackCanvas: red DamageFlash overlay + centre crosshair
//  5. TEMPORARY TrainingDummy cube in the Arena prefab
// =====================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using ARSurvival.Combat;
using ARSurvival.Player;

namespace ARSurvival.EditorTools
{
    public static class Phase4Setup
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Game.unity";
        private const string ArenaPath = Root + "/Prefabs/Arena.prefab";

        [MenuItem("Tools/AR Survival/Run Phase 4 Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase4Setup] Stop Play mode first, then run the setup again.");
                return;
            }

            Material playerMat = UnlitMaterial(Root + "/Materials/PlayerBullet.mat", new Color(0.2f, 1f, 1f));
            Material enemyMat = UnlitMaterial(Root + "/Materials/EnemyBullet.mat", new Color(1f, 0.35f, 0.1f));
            Material dummyMat = LitMaterial(Root + "/Materials/Dummy.mat", new Color(0.85f, 0.85f, 0.85f));

            Projectile playerBullet = BulletPrefab(Root + "/Prefabs/PlayerBullet.prefab", playerMat,
                                                   scale: 0.035f, speed: 9f, radius: 0.04f, trailTime: 0.12f);
            Projectile enemyBullet = BulletPrefab(Root + "/Prefabs/EnemyBullet.prefab", enemyMat,
                                                  scale: 0.06f, speed: 3.2f, radius: 0.05f, trailTime: 0.25f);

            AddDummyToArena(dummyMat);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            SetupPools(playerBullet, enemyBullet);
            SetupPlayer();
            SetupFeedbackCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00E5FF>[Phase4Setup]</color> Phase 4 setup complete.");
        }

        // ---------------------------------------------------------------
        private static Material UnlitMaterial(string path, Color color) =>
            GetOrCreateMaterial(path, "Universal Render Pipeline/Unlit", color);

        private static Material LitMaterial(string path, Color color) =>
            GetOrCreateMaterial(path, "Universal Render Pipeline/Lit", color);

        private static Material GetOrCreateMaterial(string path, string shaderName, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Projectile BulletPrefab(string path, Material mat, float scale, float speed,
                                               float radius, float trailTime)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = System.IO.Path.GetFileNameWithoutExtension(path);
            Object.DestroyImmediate(go.GetComponent<Collider>());   // movement uses sphere-casts, not physics
            go.transform.localScale = Vector3.one * scale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = mat;
            trail.time = trailTime;
            trail.startWidth = scale * 0.8f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.01f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var projectile = go.AddComponent<Projectile>();
            var so = new SerializedObject(projectile);
            so.FindProperty("speed").floatValue = speed;
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("trail").objectReferenceValue = trail;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Projectile>();
        }

        private static void AddDummyToArena(Material mat)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ArenaPath);
            if (root.GetComponentInChildren<TrainingDummy>() == null)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);   // keeps its BoxCollider (needed for hits)
                cube.name = "TrainingDummy (temporary)";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                cube.transform.localScale = Vector3.one * 0.2f;
                cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
                cube.AddComponent<TrainingDummy>();
            }
            PrefabUtility.SaveAsPrefabAsset(root, ArenaPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ---------------------------------------------------------------
        private static void SetupPools(Projectile playerBullet, Projectile enemyBullet)
        {
            GameObject pools = GameObject.Find("Pools") ?? new GameObject("Pools");
            ConfigurePool(pools.transform, "PlayerBulletPool", Team.Player, playerBullet, 30);
            ConfigurePool(pools.transform, "EnemyBulletPool", Team.Enemy, enemyBullet, 40);
        }

        private static void ConfigurePool(Transform parent, string name, Team team, Projectile prefab, int size)
        {
            Transform t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);

            var pool = go.GetComponent<ProjectilePool>();
            if (pool == null) pool = go.AddComponent<ProjectilePool>();
            var so = new SerializedObject(pool);
            so.FindProperty("owner").enumValueIndex = (int)team;
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("poolSize").intValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupPlayer()
        {
            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            if (origin == null || origin.Camera == null)
            {
                Debug.LogError("[Phase4Setup] XR Origin camera not found.");
                return;
            }
            GameObject cam = origin.Camera.gameObject;

            if (cam.GetComponent<PlayerHealth>() == null) cam.AddComponent<PlayerHealth>();
            var shooter = cam.GetComponent<PlayerShooter>();
            if (shooter == null) shooter = cam.AddComponent<PlayerShooter>();
            var so = new SerializedObject(shooter);
            so.FindProperty("aimCamera").objectReferenceValue = origin.Camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Hitbox around the phone so enemy bullets can hit the player.
            Transform hb = cam.transform.Find("PlayerHitbox");
            GameObject hitbox = hb != null ? hb.gameObject : new GameObject("PlayerHitbox");
            hitbox.transform.SetParent(cam.transform, false);
            hitbox.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var sphere = hitbox.GetComponent<SphereCollider>();
            if (sphere == null) sphere = hitbox.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.25f;
        }

        private static void SetupFeedbackCanvas()
        {
            GameObject canvasGo = GameObject.Find("FeedbackCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("FeedbackCanvas", typeof(Canvas), typeof(CanvasScaler));
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Full-screen red flash
            if (canvasGo.transform.Find("DamageFlash") == null)
            {
                var flash = new GameObject("DamageFlash", typeof(RectTransform), typeof(Image), typeof(DamageFlash));
                flash.transform.SetParent(canvasGo.transform, false);
                var rt = flash.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var img = flash.GetComponent<Image>();
                img.color = new Color(1f, 0f, 0f, 0f);
                img.raycastTarget = false;
            }

            // Crosshair (only visible while playing)
            if (canvasGo.transform.Find("Crosshair") == null)
            {
                var cross = new GameObject("Crosshair", typeof(RectTransform), typeof(CanvasGroup),
                                           typeof(ShowWhilePlaying));
                cross.transform.SetParent(canvasGo.transform, false);
                var rt = cross.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(64, 64);

                Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                AddCrosshairPart(cross.transform, "Ring", knob, 56f, new Color(1f, 1f, 1f, 0.35f));
                AddCrosshairPart(cross.transform, "Dot", knob, 12f, new Color(0.2f, 1f, 1f, 0.95f));
            }
        }

        private static void AddCrosshairPart(Transform parent, string name, Sprite sprite, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
#endif

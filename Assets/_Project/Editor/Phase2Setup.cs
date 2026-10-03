// =====================================================================
//  Phase2Setup.cs  —  AR Survival Shooter (Phase 2 automation)
//  Editor-only. Menu: Tools ▸ AR Survival ▸ Run Phase 2 Setup
//
//  1. Creates materials: NamePlane (your name texture, transparent),
//     NamePlaneEdge (cyan border), ArenaFloor (translucent disc)
//  2. Builds NamePlane.prefab from the default plane, using your texture
//  3. Builds Arena.prefab (placeholder play area)
//  4. Wires the Game scene: plane manager uses NamePlane, and
//     ARPlacementController + PlaneVisibilityController go on XR Origin
//  Safe to run more than once.
// =====================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
using ARSurvival.AR;

namespace ARSurvival.EditorTools
{
    public static class Phase2Setup
    {
        private const string Root = "Assets/_Project";
        private const string TexturePath = Root + "/Textures/name_plane.png";
        private const string NamePlaneMatPath = Root + "/Materials/NamePlane.mat";
        private const string EdgeMatPath = Root + "/Materials/NamePlaneEdge.mat";
        private const string ArenaMatPath = Root + "/Materials/ArenaFloor.mat";
        private const string TempPlanePath = Root + "/Prefabs/TempDefaultPlane.prefab";
        private const string NamePlanePath = Root + "/Prefabs/NamePlane.prefab";
        private const string ArenaPath = Root + "/Prefabs/Arena.prefab";
        private const string ScenePath = Root + "/Scenes/Game.unity";

        private static readonly Color Cyan = new Color(0f, 0.9f, 1f, 1f);

        [MenuItem("Tools/AR Survival/Run Phase 2 Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase2Setup] Stop Play mode first, then run the setup again.");
                return;
            }

            Material nameMat = CreateNamePlaneMaterial();
            Material edgeMat = CreateUnlitMaterial(EdgeMatPath, Cyan, transparent: false);
            Material arenaMat = CreateUnlitMaterial(ArenaMatPath, new Color(0f, 0.9f, 1f, 0.18f), transparent: true);

            GameObject namePlane = CreateNamePlanePrefab(nameMat, edgeMat);
            GameObject arena = CreateArenaPrefab(arenaMat, edgeMat);
            WireScene(namePlane, arena);

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00E5FF>[Phase2Setup]</color> Phase 2 setup complete.");
        }

        // ---------------------------------------------------------------
        // Materials
        // ---------------------------------------------------------------
        private static Material CreateNamePlaneMaterial()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (tex == null) Debug.LogError("[Phase2Setup] Missing texture at " + TexturePath);

            Material mat = CreateUnlitMaterial(NamePlaneMatPath, Color.white, transparent: true);
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            // ARPlaneMeshVisualizer makes UVs in metres, so 1 tile = 1 m. 0.6 m tiles read better on a floor.
            mat.SetTextureScale("_BaseMap", new Vector2(1f / 0.6f, 1f / 0.6f));
            mat.SetFloat("_Cull", (float)CullMode.Off);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateUnlitMaterial(string path, Color color, bool transparent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;

            mat.SetColor("_BaseColor", color);

            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);   // Transparent
                mat.SetFloat("_Blend", 0f);     // Alpha
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_ZWrite", 1f);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------------------------------------------------------
        // NamePlane prefab (custom plane tracker)
        // ---------------------------------------------------------------
        private static GameObject CreateNamePlanePrefab(Material fill, Material edge)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(NamePlanePath) == null)
            {
                if (!AssetDatabase.CopyAsset(TempPlanePath, NamePlanePath))
                {
                    Debug.LogError("[Phase2Setup] Could not copy " + TempPlanePath);
                    return null;
                }
            }

            GameObject root = PrefabUtility.LoadPrefabContents(NamePlanePath);
            root.name = "NamePlane";

            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = fill;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            var line = root.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.sharedMaterial = edge;
                line.startColor = line.endColor = Cyan;
                line.widthMultiplier = 0.012f;
                line.loop = true;
            }

            PrefabUtility.SaveAsPrefabAsset(root, NamePlanePath);
            PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(NamePlanePath);
        }

        // ---------------------------------------------------------------
        // Arena prefab (placeholder play area, replaced/extended later)
        // ---------------------------------------------------------------
        private static GameObject CreateArenaPrefab(Material floor, Material edge)
        {
            var root = new GameObject("Arena");

            // Translucent disc showing the play area (3 m across).
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "PlayAreaDisc";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            disc.transform.localScale = new Vector3(3f, 0.001f, 3f);
            var discRenderer = disc.GetComponent<MeshRenderer>();
            discRenderer.sharedMaterial = floor;
            discRenderer.shadowCastingMode = ShadowCastingMode.Off;

            // Small centre marker so placement is easy to see.
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "CentreMarker";
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            marker.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
            marker.GetComponent<MeshRenderer>().sharedMaterial = edge;

            // Empty used later as the parent for spawned enemies/props.
            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ArenaPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------
        // Scene wiring
        // ---------------------------------------------------------------
        private static void WireScene(GameObject namePlane, GameObject arena)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            if (origin == null)
            {
                Debug.LogError("[Phase2Setup] No XR Origin in Game scene.");
                return;
            }
            GameObject go = origin.gameObject;

            var planeManager = go.GetComponent<ARPlaneManager>();
            if (namePlane != null) planeManager.planePrefab = namePlane;

            // Explicit null checks: in the Editor, GetComponent can return a Unity "fake null", which ?? doesn't catch.
            var placement = go.GetComponent<ARPlacementController>();
            if (placement == null) placement = go.AddComponent<ARPlacementController>();
            var visibility = go.GetComponent<PlaneVisibilityController>();
            if (visibility == null) visibility = go.AddComponent<PlaneVisibilityController>();

            var so = new SerializedObject(placement);
            so.FindProperty("arenaPrefab").objectReferenceValue = arena;
            so.FindProperty("raycastManager").objectReferenceValue = go.GetComponent<ARRaycastManager>();
            so.FindProperty("planeManager").objectReferenceValue = planeManager;
            so.FindProperty("anchorManager").objectReferenceValue = go.GetComponent<ARAnchorManager>();
            so.FindProperty("arCamera").objectReferenceValue = origin.Camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            var vso = new SerializedObject(visibility);
            vso.FindProperty("planeManager").objectReferenceValue = planeManager;
            vso.FindProperty("placement").objectReferenceValue = placement;
            vso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif

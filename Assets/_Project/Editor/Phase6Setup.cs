// =====================================================================
//  Phase6Setup.cs  —  AR Survival Shooter (Phase 6 automation)
//  Editor-only. Menu: Tools ▸ AR Survival ▸ Run Phase 6 Setup
//
//  Builds the whole UI in the Game scene (portrait, 1080×1920 reference):
//    GameUI canvas ─ MainMenu, Placement, HUD, GameOver, Leaderboard panels
//    EventSystem   ─ Input System UI module (touch + mouse)
//  Adds LeaderboardService to the GameManager and removes the temporary
//  debug panel. Re-running rebuilds the UI from scratch.
// =====================================================================
#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARSurvival.Core;
using ARSurvival.Data;
using ARSurvival.UI;

namespace ARSurvival.EditorTools
{
    public static class Phase6Setup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        // Palette (matches the cyan name plane)
        private static readonly Color Dim = new Color(0.02f, 0.04f, 0.08f, 0.72f);
        private static readonly Color Card = new Color(0.05f, 0.08f, 0.13f, 0.88f);
        private static readonly Color Cyan = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color Ink = new Color(0.03f, 0.07f, 0.12f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.8f, 0.88f, 1f);
        private static readonly Color Ghost = new Color(1f, 1f, 1f, 0.12f);

        private static Sprite rounded;

        [MenuItem("Tools/AR Survival/Run Phase 6 Setup")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Phase6Setup] Stop Play mode first, then run the setup again.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            SetupGameManager();
            SetupEventSystem();

            GameObject old = GameObject.Find("GameUI");
            if (old != null) Object.DestroyImmediate(old);
            BuildUI();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#00E5FF>[Phase6Setup]</color> Phase 6 setup complete.");
        }

        // ---------------------------------------------------------------
        private static void SetupGameManager()
        {
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null) { Debug.LogError("[Phase6Setup] No GameManager in scene."); return; }

            // Remove the temporary debug panel (its script is deleted in this phase).
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gm.gameObject);

            if (gm.GetComponent<LeaderboardService>() == null) gm.gameObject.AddComponent<LeaderboardService>();
        }

        private static void SetupEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Object.DestroyImmediate(legacy);
            var module = es.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // ---------------------------------------------------------------
        private static void BuildUI()
        {
            var canvasGo = new GameObject("GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // Expand keeps the full 1080x1920 design visible on any screen shape
            // (tall iPhones, the Editor's landscape Game view), so nothing overlaps.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            UIManager ui = canvasGo.AddComponent<UIManager>();
            Transform root = canvasGo.transform;

            MainMenuPanel menu = BuildMainMenu(root, ui);
            PlacementPanel placement = BuildPlacement(root);
            HUDPanel hud = BuildHUD(root);
            GameOverPanel gameOver = BuildGameOver(root);
            LeaderboardPanel board = BuildLeaderboard(root, ui);

            var so = new SerializedObject(ui);
            so.FindProperty("mainMenu").objectReferenceValue = menu;
            so.FindProperty("placement").objectReferenceValue = placement;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("gameOver").objectReferenceValue = gameOver;
            so.FindProperty("leaderboard").objectReferenceValue = board;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------
        // Main menu
        // ---------------------------------------------------------------
        private static MainMenuPanel BuildMainMenu(Transform root, UIManager ui)
        {
            GameObject p = Panel(root, "MainMenuPanel", Dim);

            Text(Frame(p), "Kicker", "AUGMENTED REALITY", 34, Cyan, new Vector2(0.5f, 0.805f), new Vector2(900, 60), FontStyles.Bold);
            Text(Frame(p), "Title", "SURVIVAL\nSHOOTER", 130, Color.white, new Vector2(0.5f, 0.70f), new Vector2(1000, 330), FontStyles.Bold, lineSpacing: -18);
            Text(Frame(p), "Byline", "by Duru Donald Onyebuchi", 36, Muted, new Vector2(0.5f, 0.585f), new Vector2(900, 60));

            Text(Frame(p), "DifficultyLabel", "DIFFICULTY", 30, Muted, new Vector2(0.5f, 0.49f), new Vector2(900, 50), FontStyles.Bold);
            Button easy = Btn(Frame(p), "EasyButton", "EASY", Ghost, Color.white, new Vector2(0.5f, 0.435f), new Vector2(380, 120), 44, xOffset: -200);
            Button hard = Btn(Frame(p), "HardButton", "HARD", Ghost, Color.white, new Vector2(0.5f, 0.435f), new Vector2(380, 120), 44, xOffset: 200);
            TMP_Text info = Text(Frame(p), "DifficultyInfo", "", 30, Muted, new Vector2(0.5f, 0.385f), new Vector2(960, 50));

            Button start = Btn(Frame(p), "StartButton", "START", Cyan, Ink, new Vector2(0.5f, 0.27f), new Vector2(780, 170), 64);
            Button board = Btn(Frame(p), "LeaderboardButton", "LEADERBOARD", Ghost, Color.white, new Vector2(0.5f, 0.175f), new Vector2(780, 130), 44);

            Text(Frame(p), "Footer", "Scan the floor • Place the arena • Hold to shoot • Survive the timer",
                 28, Muted, new Vector2(0.5f, 0.07f), new Vector2(980, 90));

            var panel = p.AddComponent<MainMenuPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("ui").objectReferenceValue = ui;
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("leaderboardButton").objectReferenceValue = board;
            so.FindProperty("easyButton").objectReferenceValue = easy;
            so.FindProperty("hardButton").objectReferenceValue = hard;
            so.FindProperty("difficultyInfo").objectReferenceValue = info;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // ---------------------------------------------------------------
        // Placement hint
        // ---------------------------------------------------------------
        private static PlacementPanel BuildPlacement(Transform root)
        {
            GameObject p = Panel(root, "PlacementPanel", Color.clear, raycast: false);

            GameObject card = Box(Frame(p), "HintCard", Card, new Vector2(0.5f, 0.86f), new Vector2(940, 230));
            TMP_Text hint = Text(card.transform, "Hint", "Move your phone slowly\nto scan the floor", 48, Color.white,
                                 new Vector2(0.5f, 0.5f), new Vector2(880, 200));
            Button back = Btn(Frame(p), "BackButton", "BACK", Ghost, Color.white, new Vector2(0.5f, 0.07f), new Vector2(360, 110), 38);

            var panel = p.AddComponent<PlacementPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("hint").objectReferenceValue = hint;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("planeManager").objectReferenceValue = Object.FindAnyObjectByType<ARPlaneManager>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // ---------------------------------------------------------------
        // HUD (nothing here blocks taps)
        // ---------------------------------------------------------------
        private static HUDPanel BuildHUD(Transform root)
        {
            GameObject p = Panel(root, "HUDPanel", Color.clear, raycast: false);

            GameObject bar = Box(Frame(p), "TopBar", Card, new Vector2(0.5f, 0.87f), new Vector2(1000, 240), raycast: false);
            Transform b = bar.transform;

            // Health (left)
            Text(b, "HealthLabel", "HEALTH", 26, Muted, new Vector2(0.27f, 0.8f), new Vector2(440, 40), FontStyles.Bold, TextAlignmentOptions.Left);
            Box(b, "HealthBack", new Color(1f, 1f, 1f, 0.12f), new Vector2(0.27f, 0.58f), new Vector2(440, 44), raycast: false);
            GameObject fillGo = Box(b, "HealthFill", new Color(0.2f, 0.95f, 0.55f), new Vector2(0.27f, 0.58f), new Vector2(440, 44), raycast: false);
            var fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            TMP_Text hpText = Text(b, "HealthValue", "100", 34, Color.white, new Vector2(0.27f, 0.58f), new Vector2(440, 50), FontStyles.Bold);

            // Time (right)
            Text(b, "TimeLabel", "TIME LEFT", 26, Muted, new Vector2(0.78f, 0.8f), new Vector2(360, 40), FontStyles.Bold);
            TMP_Text time = Text(b, "TimeValue", "1:30", 96, Color.white, new Vector2(0.78f, 0.47f), new Vector2(360, 120), FontStyles.Bold);

            // Score + kills (bottom row)
            Text(b, "ScoreLabel", "SCORE", 26, Muted, new Vector2(0.12f, 0.2f), new Vector2(160, 40), FontStyles.Bold, TextAlignmentOptions.Left);
            TMP_Text score = Text(b, "ScoreValue", "0", 44, Cyan, new Vector2(0.3f, 0.2f), new Vector2(200, 60), FontStyles.Bold, TextAlignmentOptions.Left);
            Text(b, "KillsLabel", "KILLS", 26, Muted, new Vector2(0.5f, 0.2f), new Vector2(140, 40), FontStyles.Bold, TextAlignmentOptions.Left);
            TMP_Text kills = Text(b, "KillsValue", "0", 44, Cyan, new Vector2(0.62f, 0.2f), new Vector2(140, 60), FontStyles.Bold, TextAlignmentOptions.Left);

            Text(Frame(p), "ShootHint", "Hold anywhere to shoot", 30, new Color(1f, 1f, 1f, 0.6f),
                 new Vector2(0.5f, 0.06f), new Vector2(900, 50));

            var panel = p.AddComponent<HUDPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("healthFill").objectReferenceValue = fill;
            so.FindProperty("healthText").objectReferenceValue = hpText;
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("killsText").objectReferenceValue = kills;
            so.FindProperty("timeText").objectReferenceValue = time;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // ---------------------------------------------------------------
        // Game over
        // ---------------------------------------------------------------
        private static GameOverPanel BuildGameOver(Transform root)
        {
            GameObject p = Panel(root, "GameOverPanel", Dim);
            GameObject card = Box(Frame(p), "Card", Card, new Vector2(0.5f, 0.52f), new Vector2(940, 1200));
            Transform c = card.transform;

            TMP_Text title = Text(c, "Title", "YOU SURVIVED", 96, Color.white, new Vector2(0.5f, 0.9f), new Vector2(880, 130), FontStyles.Bold);
            TMP_Text sub = Text(c, "Subtitle", "Easy mode", 32, Muted, new Vector2(0.5f, 0.82f), new Vector2(880, 50));

            TMP_Text score = StatRow(c, "FINAL SCORE", 0.69f);
            TMP_Text kills = StatRow(c, "ENEMIES DEFEATED", 0.58f);
            TMP_Text time = StatRow(c, "TIME SURVIVED", 0.47f);

            Button restart = Btn(c, "RestartButton", "RESTART", Cyan, Ink, new Vector2(0.5f, 0.27f), new Vector2(780, 160), 58);
            Button menu = Btn(c, "MenuButton", "MAIN MENU", Ghost, Color.white, new Vector2(0.5f, 0.12f), new Vector2(780, 130), 44);

            var panel = p.AddComponent<GameOverPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("subtitle").objectReferenceValue = sub;
            so.FindProperty("scoreValue").objectReferenceValue = score;
            so.FindProperty("killsValue").objectReferenceValue = kills;
            so.FindProperty("timeValue").objectReferenceValue = time;
            so.FindProperty("restartButton").objectReferenceValue = restart;
            so.FindProperty("menuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static TMP_Text StatRow(Transform card, string label, float y)
        {
            Box(card, label + " Row", new Color(1f, 1f, 1f, 0.06f), new Vector2(0.5f, y), new Vector2(820, 110), raycast: false);
            Text(card, label + " Label", label, 34, Muted, new Vector2(0.33f, y), new Vector2(460, 60), FontStyles.Bold, TextAlignmentOptions.Left);
            return Text(card, label + " Value", "0", 60, Color.white, new Vector2(0.72f, y), new Vector2(320, 80), FontStyles.Bold, TextAlignmentOptions.Right);
        }

        // ---------------------------------------------------------------
        // Leaderboard
        // ---------------------------------------------------------------
        private static LeaderboardPanel BuildLeaderboard(Transform root, UIManager ui)
        {
            GameObject p = Panel(root, "LeaderboardPanel", Dim);
            GameObject card = Box(Frame(p), "Card", Card, new Vector2(0.5f, 0.52f), new Vector2(1000, 1300));
            Transform c = card.transform;

            Text(c, "Title", "LEADERBOARD", 84, Color.white, new Vector2(0.5f, 0.92f), new Vector2(920, 110), FontStyles.Bold);
            Text(c, "Subtitle", "Your latest 5 sessions", 32, Muted, new Vector2(0.5f, 0.86f), new Vector2(920, 50));

            TMP_Text header = Text(c, "Header", "", 26, Cyan, new Vector2(0.5f, 0.79f), new Vector2(920, 50), FontStyles.Bold, TextAlignmentOptions.Left);
            header.text = "#<pos=8%>DATE<pos=36%>MODE<pos=53%>SCORE<pos=69%>KILLS<pos=81%>TIME<pos=93%>";

            var rows = new TMP_Text[5];
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 0.71f - i * 0.095f;
                Box(c, $"RowBg{i + 1}", new Color(1f, 1f, 1f, i % 2 == 0 ? 0.07f : 0.03f), new Vector2(0.5f, y), new Vector2(940, 105), raycast: false);
                rows[i] = Text(c, $"Row{i + 1}", "", 34, Color.white, new Vector2(0.5f, y), new Vector2(920, 90), alignment: TextAlignmentOptions.Left);
            }
            TMP_Text empty = Text(c, "Empty", "No sessions yet.\nPlay a round to set your first score!", 40, Muted,
                                  new Vector2(0.5f, 0.52f), new Vector2(880, 200));

            Button back = Btn(c, "BackButton", "BACK", Ghost, Color.white, new Vector2(0.5f, 0.08f), new Vector2(560, 130), 44);

            var panel = p.AddComponent<LeaderboardPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("ui").objectReferenceValue = ui;
            var arr = so.FindProperty("rows");
            arr.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            so.FindProperty("emptyMessage").objectReferenceValue = empty;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // ---------------------------------------------------------------
        // Builders
        // ---------------------------------------------------------------
        private static GameObject Panel(Transform parent, string name, Color background, bool raycast = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            if (background.a > 0f)
            {
                var img = go.AddComponent<Image>();
                img.color = background;
                img.raycastTarget = raycast;
            }

            // Fixed 1080x1920 design frame, centred. Content anchors are relative to this,
            // so spacing never changes with the screen's aspect ratio.
            var frame = new GameObject("Frame", typeof(RectTransform));
            frame.transform.SetParent(go.transform, false);
            Place(frame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1080, 1920));
            return go;
        }

        private static Transform Frame(GameObject panel) => panel.transform.Find("Frame");

        private static GameObject Box(Transform parent, string name, Color color, Vector2 anchor, Vector2 size, bool raycast = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, size);
            var img = go.GetComponent<Image>();
            img.sprite = rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.35f;   // larger corner radius
            img.color = color;
            img.raycastTarget = raycast;
            return go;
        }

        private static TMP_Text Text(Transform parent, string name, string content, float size, Color color,
                                     Vector2 anchor, Vector2 box, FontStyles style = FontStyles.Normal,
                                     TextAlignmentOptions alignment = TextAlignmentOptions.Center, float lineSpacing = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, box);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = alignment;
            t.lineSpacing = lineSpacing;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        private static Button Btn(Transform parent, string name, string label, Color bg, Color fg,
                                  Vector2 anchor, Vector2 size, float fontSize, float xOffset = 0f)
        {
            GameObject go = Box(parent, name, bg, anchor, size);
            go.GetComponent<RectTransform>().anchoredPosition += new Vector2(xOffset, 0f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            button.colors = cb;

            TMP_Text t = Text(go.transform, "Label", label, fontSize, fg, new Vector2(0.5f, 0.5f), size, FontStyles.Bold);
            Stretch(t.rectTransform);
            return button;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif

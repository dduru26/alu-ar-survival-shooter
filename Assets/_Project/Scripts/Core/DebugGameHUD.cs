// =====================================================================
//  DebugGameHUD.cs  —  TEMPORARY test panel (removed in Phase 6)
//
//  Lets you drive the state machine before the real UI exists:
//  pick difficulty, Start, fake score/kills, die, Restart, Main Menu.
//  Uses IMGUI so it needs no Canvas setup; works in Editor and on device.
// =====================================================================
using UnityEngine;

namespace ARSurvival.Core
{
    public class DebugGameHUD : MonoBehaviour
    {
        [SerializeField] private bool show = true;

        private GUIStyle label;
        private GUIStyle button;

        private void OnGUI()
        {
            GameManager gm = GameManager.Instance;
            if (!show || gm == null) return;

            // Scale the panel so it's readable on a phone and in the Editor.
            float scale = Mathf.Max(1f, Screen.height / 1100f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 22 };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 22 };

            GUILayout.BeginArea(new Rect(16, 60, 360, 600), GUI.skin.box);
            GUILayout.Label($"State: {gm.State}", label);
            GUILayout.Label($"Difficulty: {(gm.Difficulty != null ? gm.Difficulty.DisplayName : "-")}", label);

            if (gm.Session != null)
            {
                GUILayout.Label($"Time left: {gm.Session.TimeRemaining:0.0}s", label);
                GUILayout.Label($"Score: {gm.Session.Score}   Kills: {gm.Session.Kills}", label);
            }

            switch (gm.State)
            {
                case GameStateId.Menu:
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < (gm.Difficulties?.Length ?? 0); i++)
                    {
                        string n = gm.Difficulties[i] != null ? gm.Difficulties[i].DisplayName : $"#{i}";
                        if (GUILayout.Button(i == gm.DifficultyIndex ? $"[{n}]" : n, button, GUILayout.Height(50)))
                            gm.SelectDifficulty(i);
                    }
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("START", button, GUILayout.Height(60))) gm.StartGame();
                    break;

                case GameStateId.Placement:
                    GUILayout.Label("Scan the floor, then tap it to place the arena.", label);
                    break;

                case GameStateId.Playing:
                    if (GUILayout.Button("+ Kill (10 pts)", button, GUILayout.Height(50))) gm.RegisterKill(10);
                    if (GUILayout.Button("Die (end round)", button, GUILayout.Height(50))) gm.EndRound(false);
                    break;

                case GameStateId.GameOver:
                    GUILayout.Label(gm.Session.Survived ? "SURVIVED!" : "YOU DIED", label);
                    GUILayout.Label($"Time survived: {gm.Session.TimeSurvived:0.0}s", label);
                    if (GUILayout.Button("Restart", button, GUILayout.Height(50))) gm.Restart();
                    if (GUILayout.Button("Main Menu", button, GUILayout.Height(50))) gm.ReturnToMenu();
                    break;
            }
            GUILayout.EndArea();
        }
    }
}

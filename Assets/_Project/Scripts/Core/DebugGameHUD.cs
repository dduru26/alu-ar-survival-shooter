// =====================================================================
//  DebugGameHUD.cs  —  TEMPORARY test panel (removed in Phase 6)
//
//  Lets you drive the state machine before the real UI exists:
//  pick difficulty, Start, fake score/kills, take damage, die,
//  Restart, Main Menu. Also shows health and bullet-pool counts.
//  Uses IMGUI so it needs no Canvas setup; works in Editor and on device.
// =====================================================================
using UnityEngine;
using ARSurvival.Combat;
using ARSurvival.Enemies;
using ARSurvival.Player;

namespace ARSurvival.Core
{
    public class DebugGameHUD : MonoBehaviour
    {
        [SerializeField] private bool show = true;

        private GUIStyle label;
        private GUIStyle button;
        private int health, maxHealth;
        private PlayerHealth playerHealth;
        private EnemyFactory enemyFactory;

        private void OnEnable() => GameEvents.PlayerHealthChanged += OnHealth;
        private void OnDisable() => GameEvents.PlayerHealthChanged -= OnHealth;
        private void OnHealth(int current, int max) { health = current; maxHealth = max; }

        private void OnGUI()
        {
            GameManager gm = GameManager.Instance;
            if (!show || gm == null) return;

            float scale = Mathf.Max(1f, Screen.height / 1100f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 22 };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 22 };

            GUILayout.BeginArea(new Rect(16, 60, 380, 640), GUI.skin.box);
            GUILayout.Label($"State: {gm.State}", label);
            GUILayout.Label($"Difficulty: {(gm.Difficulty != null ? gm.Difficulty.DisplayName : "-")}", label);
            GUILayout.Label($"Health: {health} / {maxHealth}", label);

            if (gm.Session != null)
            {
                GUILayout.Label($"Time left: {gm.Session.TimeRemaining:0.0}s", label);
                GUILayout.Label($"Score: {gm.Session.Score}   Kills: {gm.Session.Kills}", label);
            }

            ProjectilePool pool = ProjectilePool.For(Team.Player);
            if (pool != null)
                GUILayout.Label($"Bullet pool: {pool.ActiveCount} flying / {pool.AvailableCount} ready", label);

            if (enemyFactory == null) enemyFactory = FindAnyObjectByType<EnemyFactory>();
            if (enemyFactory != null)
                GUILayout.Label($"Enemies alive: {enemyFactory.AliveCount}", label);

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
                    if (GUILayout.Button("Take 15 damage", button, GUILayout.Height(50)))
                    {
                        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
                        playerHealth?.TakeDamage(15, Vector3.zero);
                    }
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

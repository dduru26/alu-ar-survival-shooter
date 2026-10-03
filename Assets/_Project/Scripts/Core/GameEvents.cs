// =====================================================================
//  GameEvents.cs  —  Central event hub
//
//  Pattern: Observer / Event System.
//  Gameplay code raises events here; UI, audio and the leaderboard
//  subscribe. Neither side holds a reference to the other, which keeps
//  systems decoupled and easy to test in isolation.
// =====================================================================
using System;
using UnityEngine;
using ARSurvival.Enemies;

namespace ARSurvival.Core
{
    public static class GameEvents
    {
        // ---- Game flow / HUD ----------------------------------------
        public static event Action<GameStateId> StateChanged;
        public static event Action<int> ScoreChanged;
        public static event Action<int> KillsChanged;
        public static event Action<float> TimeRemainingChanged;
        public static event Action<int, int> PlayerHealthChanged;   // current, max
        public static event Action PlayerDamaged;
        public static event Action<GameSession> GameEnded;

        // ---- Enemies (used by audio + effects) ----------------------
        public static event Action<EnemyType, Vector3> EnemySpawned;
        public static event Action<EnemyType, Vector3> EnemyHit;
        public static event Action<EnemyType, Vector3> EnemyKilled;
        public static event Action<Vector3> EnemyShot;        // Shooter fired
        public static event Action<Vector3> MeleeAttacked;    // Melee hit the player

        public static void RaiseStateChanged(GameStateId state) => StateChanged?.Invoke(state);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
        public static void RaiseKillsChanged(int kills) => KillsChanged?.Invoke(kills);
        public static void RaiseTimeRemainingChanged(float seconds) => TimeRemainingChanged?.Invoke(seconds);
        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDamaged() => PlayerDamaged?.Invoke();
        public static void RaiseGameEnded(GameSession session) => GameEnded?.Invoke(session);

        public static void RaiseEnemySpawned(EnemyType type, Vector3 position) => EnemySpawned?.Invoke(type, position);
        public static void RaiseEnemyHit(EnemyType type, Vector3 position) => EnemyHit?.Invoke(type, position);
        public static void RaiseEnemyKilled(EnemyType type, Vector3 position) => EnemyKilled?.Invoke(type, position);
        public static void RaiseEnemyShot(Vector3 position) => EnemyShot?.Invoke(position);
        public static void RaiseMeleeAttacked(Vector3 position) => MeleeAttacked?.Invoke(position);
    }
}

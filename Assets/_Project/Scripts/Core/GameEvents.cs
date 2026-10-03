// =====================================================================
//  GameEvents.cs  —  Central event hub
//
//  Pattern: Observer / Event System.
//  Gameplay code raises events here; UI, audio and the leaderboard
//  subscribe. Neither side holds a reference to the other, which keeps
//  systems decoupled and easy to test in isolation.
// =====================================================================
using System;

namespace ARSurvival.Core
{
    public static class GameEvents
    {
        public static event Action<GameStateId> StateChanged;
        public static event Action<int> ScoreChanged;
        public static event Action<int> KillsChanged;
        public static event Action<float> TimeRemainingChanged;
        public static event Action<int, int> PlayerHealthChanged;   // current, max
        public static event Action PlayerDamaged;
        public static event Action<GameSession> GameEnded;

        public static void RaiseStateChanged(GameStateId state) => StateChanged?.Invoke(state);
        public static void RaiseScoreChanged(int score) => ScoreChanged?.Invoke(score);
        public static void RaiseKillsChanged(int kills) => KillsChanged?.Invoke(kills);
        public static void RaiseTimeRemainingChanged(float seconds) => TimeRemainingChanged?.Invoke(seconds);
        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDamaged() => PlayerDamaged?.Invoke();
        public static void RaiseGameEnded(GameSession session) => GameEnded?.Invoke(session);
    }
}

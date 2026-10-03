// =====================================================================
//  GameSession.cs  —  Data for one round (score, kills, time)
//
//  Encapsulation: values change only through the methods below, which
//  also raise the matching GameEvents so the HUD stays in sync.
//  [Serializable] so the leaderboard can save it as JSON later.
// =====================================================================
using System;
using UnityEngine;

namespace ARSurvival.Core
{
    [Serializable]
    public class GameSession
    {
        [SerializeField] private int score;
        [SerializeField] private int kills;
        [SerializeField] private float timeSurvived;
        [SerializeField] private bool survived;
        [SerializeField] private string difficulty;
        [SerializeField] private string dateTime;

        public int Score => score;
        public int Kills => kills;
        public float TimeSurvived => timeSurvived;
        public bool Survived => survived;
        public string Difficulty => difficulty;
        public string DateTime => dateTime;

        /// <summary>Seconds left in the round (not saved to the leaderboard).</summary>
        public float TimeRemaining { get; private set; }

        public GameSession(string difficultyName, float duration)
        {
            difficulty = difficultyName;
            TimeRemaining = duration;
        }

        public void AddScore(int amount)
        {
            score = Mathf.Max(0, score + amount);
            GameEvents.RaiseScoreChanged(score);
        }

        public void AddKill()
        {
            kills++;
            GameEvents.RaiseKillsChanged(kills);
        }

        /// <summary>Advances the round clock. Returns true when time has run out.</summary>
        public bool Tick(float deltaTime)
        {
            TimeRemaining = Mathf.Max(0f, TimeRemaining - deltaTime);
            timeSurvived += deltaTime;
            GameEvents.RaiseTimeRemainingChanged(TimeRemaining);
            return TimeRemaining <= 0f;
        }

        public void Finish(bool playerSurvived)
        {
            survived = playerSurvived;
            dateTime = System.DateTime.Now.ToString("dd MMM HH:mm");
        }
    }
}

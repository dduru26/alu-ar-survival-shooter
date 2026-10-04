using System;
using System.Collections.Generic;
using UnityEngine;
using ARSurvival.Core;

namespace ARSurvival.Data
{
    [Serializable]
    public class LeaderboardEntry
    {
        public int score;
        public int kills;
        public float timeSurvived;
        public bool survived;
        public string difficulty;
        public string dateTime;
    }

    public class LeaderboardService : MonoBehaviour
    {
        private const string PrefsKey = "ARSurvival.Leaderboard.v1";

        [Tooltip("How many recent sessions to keep.")]
        [SerializeField, Min(1)] private int maxEntries = 5;

        public static LeaderboardService Instance { get; private set; }

        public IReadOnlyList<LeaderboardEntry> Entries => data.entries;
        public event Action Changed;

        [Serializable]
        private class LeaderboardData
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }

        private LeaderboardData data = new LeaderboardData();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Load();
        }

        private void OnEnable() => GameEvents.GameEnded += RecordSession;
        private void OnDisable() => GameEvents.GameEnded -= RecordSession;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RecordSession(GameSession session)
        {
            if (session == null) return;

            data.entries.Insert(0, new LeaderboardEntry
            {
                score = session.Score,
                kills = session.Kills,
                timeSurvived = session.TimeSurvived,
                survived = session.Survived,
                difficulty = session.Difficulty,
                dateTime = session.DateTime
            });

            while (data.entries.Count > maxEntries)
                data.entries.RemoveAt(data.entries.Count - 1);

            Save();
            Changed?.Invoke();
        }

        public void ClearAll()
        {
            data.entries.Clear();
            Save();
            Changed?.Invoke();
        }

        private void Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) { data = new LeaderboardData(); return; }

            try
            {
                data = JsonUtility.FromJson<LeaderboardData>(json) ?? new LeaderboardData();
                data.entries ??= new List<LeaderboardEntry>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LeaderboardService] Could not read saved leaderboard, starting fresh. {e.Message}");
                data = new LeaderboardData();
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}

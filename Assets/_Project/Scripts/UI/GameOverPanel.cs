using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARSurvival.Core;
using ARSurvival.Data;

namespace ARSurvival.UI
{
    public class GameOverPanel : UIPanel
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text scoreValue;
        [SerializeField] private TMP_Text killsValue;
        [SerializeField] private TMP_Text timeValue;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        protected override void Awake()
        {
            base.Awake();
            restartButton.onClick.AddListener(() => GameManager.Instance?.Restart());
            menuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
        }

        protected override void OnShow()
        {
            GameSession s = GameManager.Instance != null ? GameManager.Instance.Session : null;
            if (s == null) return;

            title.text = s.Survived ? "YOU SURVIVED" : "YOU DIED";
            title.color = s.Survived ? new Color(0.2f, 0.95f, 0.55f) : new Color(1f, 0.3f, 0.25f);
            subtitle.text = $"{s.Difficulty} mode  •  saved to leaderboard";
            scoreValue.text = s.Score.ToString();
            killsValue.text = s.Kills.ToString();
            timeValue.text = FormatTime(s.TimeSurvived);
        }
    }
}

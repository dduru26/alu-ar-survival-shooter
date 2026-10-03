// =====================================================================
//  HUDPanel.cs  —  In-game HUD: health, score, time remaining and kills (Observer on GameEvents).
//  Extends UIPanel (inheritance) and overrides OnShow (polymorphism).
// =====================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARSurvival.Core;
using ARSurvival.Data;

namespace ARSurvival.UI
{
    public class HUDPanel : UIPanel
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text timeText;

        [SerializeField] private Color healthyColor = new Color(0.2f, 0.95f, 0.55f);
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.25f, 0.2f);

        protected override bool BlocksRaycasts => false;   // taps must reach the shooter

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealth;
            GameEvents.ScoreChanged += OnScore;
            GameEvents.KillsChanged += OnKills;
            GameEvents.TimeRemainingChanged += OnTime;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealth;
            GameEvents.ScoreChanged -= OnScore;
            GameEvents.KillsChanged -= OnKills;
            GameEvents.TimeRemainingChanged -= OnTime;
        }

        private void OnHealth(int current, int max)
        {
            float t = max > 0 ? (float)current / max : 0f;
            if (healthFill != null)
            {
                healthFill.fillAmount = t;
                healthFill.color = Color.Lerp(lowHealthColor, healthyColor, t);
            }
            if (healthText != null) healthText.text = $"{current}";
        }

        private void OnScore(int score) { if (scoreText != null) scoreText.text = score.ToString(); }
        private void OnKills(int kills) { if (killsText != null) killsText.text = kills.ToString(); }

        private void OnTime(float seconds)
        {
            if (timeText == null) return;
            timeText.text = FormatTime(seconds);
            timeText.color = seconds <= 10f ? lowHealthColor : Color.white;
        }
    }
}

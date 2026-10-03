// =====================================================================
//  MainMenuPanel.cs  —  Start menu: title, difficulty (Easy/Hard), Start and Leaderboard buttons.
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
    public class MainMenuPanel : UIPanel
    {
        [SerializeField] private UIManager ui;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private TMP_Text difficultyInfo;

        [SerializeField] private Color selectedColor = new Color(0f, 0.9f, 1f, 1f);
        [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.12f);

        protected override void Awake()
        {
            base.Awake();
            startButton.onClick.AddListener(() => GameManager.Instance?.StartGame());
            leaderboardButton.onClick.AddListener(() => ui.ShowLeaderboard());
            easyButton.onClick.AddListener(() => SelectDifficulty(0));
            hardButton.onClick.AddListener(() => SelectDifficulty(1));
        }

        protected override void OnShow() => Refresh();

        private void SelectDifficulty(int index)
        {
            GameManager.Instance?.SelectDifficulty(index);
            Refresh();
        }

        private void Refresh()
        {
            GameManager gm = GameManager.Instance;
            int index = gm != null ? gm.DifficultyIndex : 0;
            Paint(easyButton, index == 0);
            Paint(hardButton, index == 1);

            DifficultySettings d = gm != null ? gm.Difficulty : null;
            if (difficultyInfo != null && d != null)
                difficultyInfo.text = $"Survive {FormatTime(d.RoundDuration)}  •  {d.PlayerMaxHealth} HP  •  up to {d.MaxAliveEnemies} enemies";
        }

        private void Paint(Button b, bool selected)
        {
            if (b.targetGraphic != null) b.targetGraphic.color = selected ? selectedColor : unselectedColor;
            var label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = selected ? new Color(0.03f, 0.07f, 0.12f) : Color.white;
        }
    }
}

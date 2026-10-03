// =====================================================================
//  LeaderboardPanel.cs  —  Shows the latest 5 saved sessions (newest first) with a Back button.
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
    public class LeaderboardPanel : UIPanel
    {
        [SerializeField] private UIManager ui;
        [SerializeField] private TMP_Text[] rows;
        [SerializeField] private TMP_Text emptyMessage;
        [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(() => ui.ShowMainMenu());
        }

        protected override void OnShow()
        {
            var entries = LeaderboardService.Instance != null ? LeaderboardService.Instance.Entries : null;
            int count = entries?.Count ?? 0;
            if (emptyMessage != null) emptyMessage.gameObject.SetActive(count == 0);

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                bool has = i < count;
                rows[i].gameObject.SetActive(has);
                if (!has) continue;

                LeaderboardEntry e = entries[i];
                string result = e.survived ? "<color=#33F28C>WIN</color>" : "<color=#FF4D40>KO</color>";
                rows[i].text =
                    $"<b>{i + 1}</b><pos=8%>{e.dateTime}<pos=36%>{e.difficulty}<pos=53%><b>{e.score}</b>" +
                    $"<pos=69%>{e.kills}<pos=81%>{FormatTime(e.timeSurvived)}<pos=93%>{result}";
            }
        }
    }
}

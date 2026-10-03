// =====================================================================
//  UIManager.cs  —  Decides which screen is visible
//
//  Observer: listens to GameEvents.StateChanged and shows the matching
//  panel (Menu → MainMenu, Placement → Placement, Playing → HUD,
//  GameOver → GameOver). The Leaderboard is opened from the menu.
//  Separation of concerns: panels never switch each other; they ask
//  the GameManager to change state, and the UIManager reacts.
// =====================================================================
using UnityEngine;
using ARSurvival.Core;

namespace ARSurvival.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private MainMenuPanel mainMenu;
        [SerializeField] private PlacementPanel placement;
        [SerializeField] private HUDPanel hud;
        [SerializeField] private GameOverPanel gameOver;
        [SerializeField] private LeaderboardPanel leaderboard;

        private UIPanel[] panels;

        private void Awake()
        {
            panels = new UIPanel[] { mainMenu, placement, hud, gameOver, leaderboard };
        }

        private void OnEnable() => GameEvents.StateChanged += OnStateChanged;
        private void OnDisable() => GameEvents.StateChanged -= OnStateChanged;

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            OnStateChanged(gm != null ? gm.State : GameStateId.Menu);
        }

        private void OnStateChanged(GameStateId state)
        {
            switch (state)
            {
                case GameStateId.Menu:      ShowOnly(mainMenu); break;
                case GameStateId.Placement: ShowOnly(placement); break;
                case GameStateId.Playing:   ShowOnly(hud); break;
                case GameStateId.GameOver:  ShowOnly(gameOver); break;
            }
        }

        public void ShowLeaderboard() => ShowOnly(leaderboard);
        public void ShowMainMenu() => ShowOnly(mainMenu);

        private void ShowOnly(UIPanel target)
        {
            foreach (UIPanel p in panels)
            {
                if (p == null) continue;
                if (p == target) p.Show();
                else p.Hide();
            }
        }
    }
}

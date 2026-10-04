using System;
using UnityEngine;
using ARSurvival.AR;

namespace ARSurvival.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Difficulty")]
        [Tooltip("Index 0 = Easy, 1 = Hard (bonus difficulty levels).")]
        [SerializeField] private DifficultySettings[] difficulties;
        [SerializeField] private int defaultDifficultyIndex;

        [Header("References (auto-found if empty)")]
        [SerializeField] private ARPlacementController placement;
        [SerializeField] private PlaneVisibilityController planeVisibility;

        public event Action RoundStarted;
        public event Action RoundEnded;

        public ARPlacementController Placement => placement;
        public DifficultySettings Difficulty => difficulties != null && difficulties.Length > 0
            ? difficulties[Mathf.Clamp(difficultyIndex, 0, difficulties.Length - 1)]
            : null;
        public DifficultySettings[] Difficulties => difficulties;
        public int DifficultyIndex => difficultyIndex;
        public GameSession Session { get; private set; }
        public GameStateId State => stateMachine.Current?.Id ?? GameStateId.Menu;
        public bool IsPlaying => State == GameStateId.Playing;
        public Transform Arena => placement != null ? placement.PlacedArena : null;

        private readonly GameStateMachine stateMachine = new GameStateMachine();
        private MenuState menuState;
        private PlacementState placementState;
        private PlayingState playingState;
        private GameOverState gameOverState;
        private int difficultyIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate found — destroying the extra one.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (placement == null) placement = FindAnyObjectByType<ARPlacementController>();
            if (planeVisibility == null) planeVisibility = FindAnyObjectByType<PlaneVisibilityController>();

            difficultyIndex = defaultDifficultyIndex;
            menuState = new MenuState(this);
            placementState = new PlacementState(this);
            playingState = new PlayingState(this);
            gameOverState = new GameOverState(this);
        }

        private void Start() => stateMachine.ChangeState(menuState);

        private void Update() => stateMachine.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SelectDifficulty(int index)
        {
            if (difficulties == null || difficulties.Length == 0) return;
            difficultyIndex = Mathf.Clamp(index, 0, difficulties.Length - 1);
        }

        public void StartGame()
        {
            if (placement != null && placement.IsPlaced) BeginRound();
            else stateMachine.ChangeState(placementState);
        }

        public void Restart() => BeginRound();

        public void ReturnToMenu()
        {
            if (IsPlaying) EndRoundInternal(false, raiseGameEnded: false);
            stateMachine.ChangeState(menuState);
        }

        public void AddScore(int amount)
        {
            if (IsPlaying) Session.AddScore(amount);
        }

        public void RegisterKill(int points)
        {
            if (!IsPlaying) return;
            Session.AddKill();
            Session.AddScore(points);
        }

        internal void BeginRound()
        {
            DifficultySettings d = Difficulty;
            float duration = d != null ? d.RoundDuration : 90f;
            string name = d != null ? d.DisplayName : "Normal";

            Session = new GameSession(name, duration);
            GameEvents.RaiseScoreChanged(0);
            GameEvents.RaiseKillsChanged(0);
            GameEvents.RaiseTimeRemainingChanged(duration);

            stateMachine.ChangeState(playingState);
            RoundStarted?.Invoke();
        }

        public void EndRound(bool playerSurvived)
        {
            if (!IsPlaying) return;
            EndRoundInternal(playerSurvived, raiseGameEnded: true);
            stateMachine.ChangeState(gameOverState);
        }

        private void EndRoundInternal(bool playerSurvived, bool raiseGameEnded)
        {
            Session.Finish(playerSurvived);
            RoundEnded?.Invoke();
            if (raiseGameEnded) GameEvents.RaiseGameEnded(Session);
        }
    }
}

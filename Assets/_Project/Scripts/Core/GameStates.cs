// =====================================================================
//  GameStates.cs  —  State pattern for the game loop
//
//      Menu ──Start──▶ Placement ──arena placed──▶ Playing ──▶ GameOver
//        ▲                                            ▲            │
//        └──────────────── Main Menu ─────────────────┴─ Restart ──┘
//
//  • Abstraction   – IGameState defines what every state can do.
//  • Inheritance   – each concrete state extends GameStateBase.
//  • Polymorphism  – GameStateMachine calls Enter/Tick/Exit without
//                    knowing which concrete state it holds.
// =====================================================================
namespace ARSurvival.Core
{
    public enum GameStateId { Menu, Placement, Playing, GameOver }

    public interface IGameState
    {
        GameStateId Id { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>Shared base: every state can reach the GameManager.</summary>
    public abstract class GameStateBase : IGameState
    {
        protected readonly GameManager Game;
        protected GameStateBase(GameManager game) { Game = game; }

        public abstract GameStateId Id { get; }
        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }

    // -----------------------------------------------------------------
    /// <summary>Start menu: nothing moves, taps don't place anything.</summary>
    public class MenuState : GameStateBase
    {
        public MenuState(GameManager game) : base(game) { }
        public override GameStateId Id => GameStateId.Menu;

        public override void Enter() => Game.Placement?.SetAcceptingTaps(false);
    }

    // -----------------------------------------------------------------
    /// <summary>Player scans the floor and taps to place the arena.</summary>
    public class PlacementState : GameStateBase
    {
        public PlacementState(GameManager game) : base(game) { }
        public override GameStateId Id => GameStateId.Placement;

        public override void Enter()
        {
            if (Game.Placement == null) return;
            Game.Placement.ArenaPlaced += OnArenaPlaced;
            Game.Placement.SetAcceptingTaps(true);
        }

        public override void Exit()
        {
            if (Game.Placement == null) return;
            Game.Placement.ArenaPlaced -= OnArenaPlaced;
            Game.Placement.SetAcceptingTaps(false);
        }

        private void OnArenaPlaced(UnityEngine.Transform arena) => Game.BeginRound();
    }

    // -----------------------------------------------------------------
    /// <summary>The round is live: the clock counts down to a win.</summary>
    public class PlayingState : GameStateBase
    {
        public PlayingState(GameManager game) : base(game) { }
        public override GameStateId Id => GameStateId.Playing;

        public override void Tick(float deltaTime)
        {
            bool timeUp = Game.Session.Tick(deltaTime);
            if (timeUp) Game.EndRound(playerSurvived: true);
        }
    }

    // -----------------------------------------------------------------
    /// <summary>Round finished: results are shown, waiting for Restart / Menu.</summary>
    public class GameOverState : GameStateBase
    {
        public GameOverState(GameManager game) : base(game) { }
        public override GameStateId Id => GameStateId.GameOver;
    }

    // -----------------------------------------------------------------
    /// <summary>Holds the current state and handles transitions.</summary>
    public class GameStateMachine
    {
        public IGameState Current { get; private set; }

        public void ChangeState(IGameState next)
        {
            if (next == null || next == Current) return;
            Current?.Exit();
            Current = next;
            Current.Enter();
            GameEvents.RaiseStateChanged(Current.Id);
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}

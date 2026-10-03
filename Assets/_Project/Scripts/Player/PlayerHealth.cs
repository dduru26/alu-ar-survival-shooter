// =====================================================================
//  PlayerHealth.cs  —  The player's health (lives on the AR camera)
//
//  In this AR game the phone IS the player, so the camera carries the
//  health and a small hitbox collider. Implements IDamageable so enemy
//  bullets and melee attacks can hurt it without knowing it's the player.
//
//  On damage: updates the HUD (GameEvents), triggers the red screen
//  flash and a phone vibration. At 0 HP it ends the round (game over).
// =====================================================================
using System;
using UnityEngine;
using ARSurvival.Combat;
using ARSurvival.Core;

namespace ARSurvival.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Tooltip("Used only if no difficulty asset is assigned in the GameManager.")]
        [SerializeField, Min(1)] private int fallbackMaxHealth = 100;
        [Tooltip("Short window after a hit where further damage is ignored (prevents instant shred).")]
        [SerializeField, Min(0f)] private float invulnerabilityAfterHit = 0.25f;
        [SerializeField] private bool vibrateOnHit = true;

        public Team Team => Team.Player;
        public bool IsAlive => Current > 0;
        public int Current { get; private set; }
        public int Max { get; private set; }

        /// <summary>Raised when health reaches zero (audio hooks into this for the death sound).</summary>
        public event Action Died;

        private GameManager game;
        private float invulnerableUntil;

        private void Start()
        {
            game = GameManager.Instance;
            if (game != null) game.RoundStarted += ResetHealth;
            ResetHealth();
        }

        private void OnDestroy()
        {
            if (game != null) game.RoundStarted -= ResetHealth;
        }

        public void ResetHealth()
        {
            Max = game != null && game.Difficulty != null ? game.Difficulty.PlayerMaxHealth : fallbackMaxHealth;
            Current = Max;
            invulnerableUntil = 0f;
            GameEvents.RaisePlayerHealthChanged(Current, Max);
        }

        public void TakeDamage(int amount, Vector3 hitPoint)
        {
            if (amount <= 0 || !IsAlive) return;
            if (game == null || !game.IsPlaying) return;      // no damage outside a live round
            if (Time.time < invulnerableUntil) return;

            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerabilityAfterHit;

            GameEvents.RaisePlayerHealthChanged(Current, Max);
            GameEvents.RaisePlayerDamaged();

#if UNITY_IOS || UNITY_ANDROID
            if (vibrateOnHit) Handheld.Vibrate();
#endif

            if (Current == 0)
            {
                Died?.Invoke();
                game.EndRound(playerSurvived: false);
            }
        }
    }
}

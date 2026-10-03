// =====================================================================
//  MeleeEnemy.cs  —  Walks up to the player and hits at close range
//
//  • Moves toward the player
//  • Short attack range (only damages in close proximity)
//  • Attack cooldown
//  • 2 HP → destroyed by 2 player bullets
// =====================================================================
using UnityEngine;
using ARSurvival.Core;

namespace ARSurvival.Enemies
{
    public class MeleeEnemy : Enemy
    {
        [Header("Melee attack")]
        [Tooltip("Floor distance (m) at which the enemy can hit the player.")]
        [SerializeField, Min(0.1f)] private float attackRange = 0.5f;
        [SerializeField, Min(1)] private int attackDamage = 10;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;
        [Tooltip("Optional child that lunges forward when attacking.")]
        [SerializeField] private Transform lungeVisual;

        private float nextAttackTime;
        private float lungeTimer;
        private Vector3 lungeRestPosition;

        public override EnemyType Type => EnemyType.Melee;

        protected override void Awake()
        {
            base.Awake();
            if (lungeVisual != null) lungeRestPosition = lungeVisual.localPosition;
        }

        protected override void OnSpawned()
        {
            nextAttackTime = Time.time + 0.6f;   // brief grace period after spawning
            lungeTimer = 0f;
            if (lungeVisual != null) lungeVisual.localPosition = lungeRestPosition;
        }

        protected override void Behave(float deltaTime, float distanceToPlayer)
        {
            AnimateLunge(deltaTime);

            if (distanceToPlayer > attackRange)
            {
                MoveTowardPlayer(attackRange * 0.8f, deltaTime);
                return;
            }

            if (Time.time >= nextAttackTime) Attack();
        }

        private void Attack()
        {
            nextAttackTime = Time.time + attackCooldown;
            lungeTimer = 0.25f;

            int damage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * DamageMultiplier));
            PlayerTarget?.TakeDamage(damage, transform.position);
            GameEvents.RaiseMeleeAttacked(transform.position);
        }

        private void AnimateLunge(float deltaTime)
        {
            if (lungeVisual == null) return;
            if (lungeTimer > 0f) lungeTimer -= deltaTime;
            float t = Mathf.Clamp01(lungeTimer / 0.25f);
            float forward = Mathf.Sin(t * Mathf.PI) * 0.12f;
            lungeVisual.localPosition = lungeRestPosition + Vector3.forward * forward;
        }
    }
}

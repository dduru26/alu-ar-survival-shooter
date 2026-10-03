// =====================================================================
//  ShooterEnemy.cs  —  Keeps its distance and fires projectiles
//
//  • Moves toward the player, then STOPS at a preferred shooting distance
//  • Fires pooled enemy bullets at the player (longer range than melee)
//  • 4 HP → destroyed by 4 player bullets
// =====================================================================
using UnityEngine;
using ARSurvival.Combat;
using ARSurvival.Core;

namespace ARSurvival.Enemies
{
    public class ShooterEnemy : Enemy
    {
        [Header("Ranged attack")]
        [Tooltip("Floor distance (m) where it stops walking and holds position.")]
        [SerializeField, Min(0.3f)] private float shootingDistance = 1.6f;
        [Tooltip("Max floor distance (m) it will fire from. Must be longer than the melee range.")]
        [SerializeField, Min(0.5f)] private float shootRange = 2.6f;
        [SerializeField, Min(0.2f)] private float fireInterval = 1.8f;
        [SerializeField, Min(1)] private int bulletDamage = 8;
        [Tooltip("Where bullets leave the enemy.")]
        [SerializeField] private Transform muzzle;

        [Header("Hover")]
        [SerializeField] private Transform hoverVisual;
        [SerializeField, Min(0f)] private float hoverAmplitude = 0.03f;

        private float nextShotTime;
        private Vector3 hoverRestPosition;
        private float hoverPhase;

        public override EnemyType Type => EnemyType.Shooter;

        protected override void Awake()
        {
            base.Awake();
            if (hoverVisual != null) hoverRestPosition = hoverVisual.localPosition;
        }

        protected override void OnSpawned()
        {
            nextShotTime = Time.time + Random.Range(1.0f, 1.8f);   // don't fire the instant it appears
            hoverPhase = Random.value * Mathf.PI * 2f;
        }

        protected override void Behave(float deltaTime, float distanceToPlayer)
        {
            Hover();
            MoveTowardPlayer(shootingDistance, deltaTime);

            if (distanceToPlayer <= shootRange && Time.time >= nextShotTime)
            {
                nextShotTime = Time.time + fireInterval * Random.Range(0.85f, 1.15f);
                Shoot();
            }
        }

        private void Shoot()
        {
            ProjectilePool pool = ProjectilePool.For(Team.Enemy);
            if (pool == null) return;

            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.35f;
            Vector3 target = Player.position + Vector3.down * 0.1f;     // aim at the phone/chest
            Vector3 direction = (target - origin).normalized;
            int damage = Mathf.Max(1, Mathf.RoundToInt(bulletDamage * DamageMultiplier));

            if (pool.Spawn(origin, direction, damage) != null)
                GameEvents.RaiseEnemyShot(origin);
        }

        private void Hover()
        {
            if (hoverVisual == null) return;
            float y = Mathf.Sin(Time.time * 3f + hoverPhase) * hoverAmplitude;
            hoverVisual.localPosition = hoverRestPosition + Vector3.up * y;
        }
    }
}

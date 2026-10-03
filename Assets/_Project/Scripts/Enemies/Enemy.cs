// =====================================================================
//  Enemy.cs  —  Abstract base class for every enemy
//
//  OOP:
//   • Abstraction   – Enemy is abstract; you can't spawn a "plain" enemy.
//   • Inheritance   – MeleeEnemy and ShooterEnemy extend this class and
//                     reuse its health, movement, hit flash and death code.
//   • Polymorphism  – each subclass overrides Behave() with its own attack.
//   • Encapsulation – health and state are private; subclasses use the
//                     protected helpers (MoveTowardPlayer, PlayerTarget…).
//
//  Enemies are pooled by the EnemyFactory (IPoolable) and always stay on
//  the floor height of the placed arena (the detected AR plane).
// =====================================================================
using UnityEngine;
using ARSurvival.Combat;
using ARSurvival.Core;

namespace ARSurvival.Enemies
{
    public enum EnemyType { Melee, Shooter }

    public abstract class Enemy : MonoBehaviour, IDamageable, IPoolable
    {
        [Header("Stats")]
        [Tooltip("Player bullets deal 1 damage, so this is also 'bullets needed to destroy'.")]
        [SerializeField, Min(1)] private int maxHealth = 2;
        [SerializeField, Min(0.05f)] private float moveSpeed = 0.5f;
        [SerializeField, Min(0)] private int scoreValue = 10;
        [SerializeField, Min(1f)] private float turnSpeed = 8f;

        [Header("Feedback")]
        [Tooltip("Renderers that flash when the enemy is hit.")]
        [SerializeField] private Renderer[] flashRenderers;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField, Min(0.05f)] private float spawnGrowTime = 0.35f;

        public abstract EnemyType Type { get; }
        public Team Team => Team.Enemy;
        public bool IsAlive => initialized && currentHealth > 0;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        /// <summary>The player's camera (the phone).</summary>
        protected Transform Player { get; private set; }
        /// <summary>The player's health component, used by attacks.</summary>
        protected IDamageable PlayerTarget { get; private set; }
        protected float SpeedMultiplier { get; private set; } = 1f;
        protected float DamageMultiplier { get; private set; } = 1f;
        protected float FloorY { get; private set; }

        private EnemyFactory factory;
        private int currentHealth;
        private bool initialized;
        private float flashTimer;
        private float spawnTimer;
        private Vector3 baseScale;
        private MaterialPropertyBlock block;
        private Color[] baseColors;

        // -----------------------------------------------------------------
        protected virtual void Awake()
        {
            baseScale = transform.localScale;
            block = new MaterialPropertyBlock();
            baseColors = new Color[flashRenderers?.Length ?? 0];
            for (int i = 0; i < baseColors.Length; i++)
            {
                Material m = flashRenderers[i] != null ? flashRenderers[i].sharedMaterial : null;
                baseColors[i] = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            }
        }

        /// <summary>Called by the factory each time this enemy is (re)used.</summary>
        public void Initialize(EnemyFactory owner, Transform player, float floorY,
                               float speedMultiplier, float damageMultiplier)
        {
            factory = owner;
            Player = player;
            PlayerTarget = player != null ? player.GetComponentInParent<IDamageable>() : null;
            FloorY = floorY;
            SpeedMultiplier = speedMultiplier;
            DamageMultiplier = damageMultiplier;

            currentHealth = maxHealth;
            spawnTimer = 0f;
            flashTimer = 0f;
            transform.localScale = baseScale * 0.01f;
            SetFlash(false);
            initialized = true;

            OnSpawned();
            GameEvents.RaiseEnemySpawned(Type, transform.position);
        }

        // ---- IPoolable -----------------------------------------------------
        public void OnTakenFromPool() { }

        public void OnReturnedToPool()
        {
            initialized = false;
            transform.localScale = baseScale;
            SetFlash(false);
        }

        // -----------------------------------------------------------------
        private void Update()
        {
            if (!initialized) return;
            float dt = Time.deltaTime;

            // Spawn "grow in" so new enemies are easy to notice.
            if (spawnTimer < spawnGrowTime)
            {
                spawnTimer += dt;
                transform.localScale = baseScale * Mathf.SmoothStep(0.01f, 1f, spawnTimer / spawnGrowTime);
            }

            if (flashTimer > 0f && (flashTimer -= dt) <= 0f) SetFlash(false);

            GameManager game = GameManager.Instance;
            if (game == null || !game.IsPlaying || Player == null) return;

            Vector3 toPlayer = FlatToPlayer();
            FacePlayer(toPlayer, dt);
            Behave(dt, toPlayer.magnitude);
        }

        /// <summary>Per-frame behaviour. distanceToPlayer is measured along the floor.</summary>
        protected abstract void Behave(float deltaTime, float distanceToPlayer);

        /// <summary>Optional hook when the enemy (re)spawns.</summary>
        protected virtual void OnSpawned() { }

        // ---- Shared helpers for subclasses ----------------------------------
        protected Vector3 FlatToPlayer()
        {
            Vector3 d = Player.position - transform.position;
            d.y = 0f;
            return d;
        }

        /// <summary>Walks along the floor toward the player, stopping at stopDistance.</summary>
        protected void MoveTowardPlayer(float stopDistance, float deltaTime)
        {
            Vector3 d = FlatToPlayer();
            float dist = d.magnitude;
            if (dist <= stopDistance || dist < 0.0001f) return;

            float step = Mathf.Min(moveSpeed * SpeedMultiplier * deltaTime, dist - stopDistance);
            Vector3 p = transform.position + d / dist * step;
            p.y = FloorY;                                    // stay on the AR plane
            transform.position = p;
        }

        private void FacePlayer(Vector3 flatDirection, float deltaTime)
        {
            if (flatDirection.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * deltaTime);
        }

        // ---- IDamageable ---------------------------------------------------
        public void TakeDamage(int amount, Vector3 hitPoint)
        {
            if (!IsAlive || amount <= 0) return;

            currentHealth -= amount;
            flashTimer = 0.1f;
            SetFlash(true);
            GameEvents.RaiseEnemyHit(Type, hitPoint);

            if (currentHealth <= 0) Die();
        }

        protected virtual void Die()
        {
            Vector3 pos = transform.position;
            GameEvents.RaiseEnemyKilled(Type, pos);
            if (GameManager.Instance != null) GameManager.Instance.RegisterKill(scoreValue);
            Despawn();
        }

        /// <summary>Returns the enemy to the factory pool without awarding score.</summary>
        public void Despawn()
        {
            initialized = false;
            if (factory != null) factory.Release(this);
            else gameObject.SetActive(false);
        }

        private void SetFlash(bool on)
        {
            if (flashRenderers == null || block == null) return;
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                Renderer r = flashRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", on ? hitFlashColor : baseColors[i]);
                r.SetPropertyBlock(block);
            }
        }
    }
}

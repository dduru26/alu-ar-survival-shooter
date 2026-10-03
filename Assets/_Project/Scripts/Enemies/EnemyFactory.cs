// =====================================================================
//  EnemyFactory.cs  —  Creates enemies by type
//
//  PATTERN: Factory. The spawner asks for "a Shooter at this position";
//  the factory decides which prefab to use and how to build it. Behind
//  the factory, each type has its own object pool (pre-warmed), so
//  enemies are recycled instead of Instantiated/Destroyed every spawn.
//
//  When a round ends, every live enemy is wiped (returned to its pool).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using ARSurvival.Combat;
using ARSurvival.Core;

namespace ARSurvival.Enemies
{
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] private MeleeEnemy meleePrefab;
        [SerializeField] private ShooterEnemy shooterPrefab;
        [Tooltip("Enemies of each type created up front.")]
        [SerializeField, Min(1)] private int prewarmPerType = 10;

        private ComponentPool<Enemy> meleePool;
        private ComponentPool<Enemy> shooterPool;
        private readonly List<Enemy> active = new List<Enemy>();
        private GameManager game;

        public int AliveCount => active.Count;
        public IReadOnlyList<Enemy> ActiveEnemies => active;

        private void Awake()
        {
            // canGrow: true — enemies are capped by DifficultySettings.MaxAliveEnemies,
            // so the pool only grows if that cap is raised above the pre-warm size.
            if (meleePrefab != null) meleePool = new ComponentPool<Enemy>(meleePrefab, prewarmPerType, transform, canGrow: true);
            if (shooterPrefab != null) shooterPool = new ComponentPool<Enemy>(shooterPrefab, prewarmPerType, transform, canGrow: true);
            if (meleePool == null || shooterPool == null)
                Debug.LogError("[EnemyFactory] Assign both enemy prefabs.", this);
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game != null) game.RoundEnded += ReleaseAll;
        }

        private void OnDestroy()
        {
            if (game != null) game.RoundEnded -= ReleaseAll;
        }

        /// <summary>Builds an enemy of the requested type, ready to fight.</summary>
        public Enemy Create(EnemyType type, Vector3 position, Quaternion rotation,
                            Transform player, float floorY, DifficultySettings difficulty)
        {
            ComponentPool<Enemy> pool = PoolFor(type);
            Enemy enemy = pool?.Get();
            if (enemy == null) return null;

            enemy.transform.SetPositionAndRotation(position, rotation);
            float speed = difficulty != null ? difficulty.EnemySpeedMultiplier : 1f;
            float damage = difficulty != null ? difficulty.EnemyDamageMultiplier : 1f;
            enemy.Initialize(this, player, floorY, speed, damage);

            active.Add(enemy);
            return enemy;
        }

        /// <summary>Returns one enemy to its pool.</summary>
        public void Release(Enemy enemy)
        {
            if (enemy == null || !active.Remove(enemy)) return;
            PoolFor(enemy.Type)?.Release(enemy);
        }

        /// <summary>Wipes every live enemy (round end, restart, main menu).</summary>
        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Enemy e = active[i];
                active.RemoveAt(i);
                PoolFor(e.Type)?.Release(e);
            }
        }

        private ComponentPool<Enemy> PoolFor(EnemyType type) =>
            type == EnemyType.Melee ? meleePool : shooterPool;
    }
}

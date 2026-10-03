// =====================================================================
//  ProjectilePool.cs  —  Scene component wrapping ComponentPool<Projectile>
//
//  One pool per team (Player bullets, Enemy bullets). Pools register
//  themselves so shooters can ask for theirs with ProjectilePool.For(team)
//  instead of needing a hand-wired reference on every enemy.
//  All bullets are created in Awake (pre-warm) and recycled forever after.
//  When a round ends, every live bullet is returned (clean slate).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using ARSurvival.Core;

namespace ARSurvival.Combat
{
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Team owner = Team.Player;
        [SerializeField] private Projectile prefab;
        [Tooltip("Bullets created up front. The pool never grows during gameplay.")]
        [SerializeField, Min(1)] private int poolSize = 30;

        private static readonly Dictionary<Team, ProjectilePool> Registry = new Dictionary<Team, ProjectilePool>();
        private ComponentPool<Projectile> pool;
        private GameManager subscribedManager;

        public Team Owner => owner;
        public int ActiveCount => pool?.CountActive ?? 0;
        public int AvailableCount => pool?.CountAvailable ?? 0;
        public int TotalCount => pool?.CountTotal ?? 0;

        /// <summary>Finds the pool for a team (null if none exists).</summary>
        public static ProjectilePool For(Team team) =>
            Registry.TryGetValue(team, out ProjectilePool p) ? p : null;

        private void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"[ProjectilePool] {name} has no projectile prefab.", this);
                return;
            }
            pool = new ComponentPool<Projectile>(prefab, poolSize, transform, canGrow: false);
            Registry[owner] = this;
        }

        private void Start()
        {
            subscribedManager = GameManager.Instance;
            if (subscribedManager != null) subscribedManager.RoundEnded += ReleaseAll;
        }

        private void OnDestroy()
        {
            if (subscribedManager != null) subscribedManager.RoundEnded -= ReleaseAll;
            if (Registry.TryGetValue(owner, out ProjectilePool p) && p == this) Registry.Remove(owner);
        }

        /// <summary>Takes a bullet from the pool and fires it. Returns null if all bullets are in flight.</summary>
        public Projectile Spawn(Vector3 position, Vector3 direction, int damage)
        {
            Projectile p = pool?.Get();
            if (p == null) return null;
            p.Launch(position, direction, damage, owner, this);
            return p;
        }

        public void Release(Projectile projectile) => pool?.Release(projectile);

        public void ReleaseAll() => pool?.ReleaseAll();
    }
}

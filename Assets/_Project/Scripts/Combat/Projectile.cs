// =====================================================================
//  Projectile.cs  —  A pooled bullet (player or enemy)
//
//  Moves itself each frame and sphere-casts ahead so fast bullets can't
//  tunnel through thin targets. On hitting an IDamageable of the other
//  team it deals damage and returns to its pool. It also returns to the
//  pool when its lifetime ends or it hits a solid surface (e.g. the floor).
//
//  Implements IPoolable so the pool can reset it between uses.
// =====================================================================
using UnityEngine;

namespace ARSurvival.Combat
{
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Flight")]
        [SerializeField, Min(0.1f)] private float speed = 9f;
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;
        [Tooltip("Collision radius for the sphere-cast (metres).")]
        [SerializeField, Min(0.005f)] private float radius = 0.04f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Optional")]
        [SerializeField] private TrailRenderer trail;

        public Team OwnerTeam { get; private set; }

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];
        private ProjectilePool ownerPool;
        private Vector3 direction;
        private int damage;
        private float age;
        private bool inFlight;

        private void Awake()
        {
            if (trail == null) trail = GetComponent<TrailRenderer>();
        }

        /// <summary>Fires the projectile. Called by ProjectilePool right after Get().</summary>
        public void Launch(Vector3 position, Vector3 dir, int damageAmount, Team team, ProjectilePool pool)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(dir));
            direction = dir.normalized;
            damage = damageAmount;
            OwnerTeam = team;
            ownerPool = pool;
            age = 0f;
            inFlight = true;
            if (trail != null) trail.Clear();   // avoid a streak from the last position it was used at
        }

        // ---- IPoolable ---------------------------------------------------
        public void OnTakenFromPool()
        {
            age = 0f;
            inFlight = false;   // stays still until Launch()
        }

        public void OnReturnedToPool()
        {
            inFlight = false;
            damage = 0;
            ownerPool = null;
            if (trail != null) trail.Clear();
        }

        // ------------------------------------------------------------------
        private void Update()
        {
            if (!inFlight) return;

            float dt = Time.deltaTime;
            age += dt;
            if (age >= lifetime) { Despawn(); return; }

            float step = speed * dt;
            if (TryHit(step)) return;
            transform.position += direction * step;
        }

        /// <summary>Checks the path ahead. Returns true if the projectile was consumed.</summary>
        private bool TryHit(float distance)
        {
            int count = Physics.SphereCastNonAlloc(transform.position, radius, direction, HitBuffer,
                                                   distance, hitMask, QueryTriggerInteraction.Collide);
            if (count == 0) return false;

            // Find the closest hit that isn't on our own team.
            float best = float.MaxValue;
            int bestIndex = -1;
            IDamageable bestTarget = null;
            for (int i = 0; i < count; i++)
            {
                Collider col = HitBuffer[i].collider;
                var target = col.GetComponentInParent<IDamageable>();
                if (target != null && target.Team == OwnerTeam) continue;   // friendly — pass through
                if (target == null && col.isTrigger) continue;              // ignore non-combat triggers
                if (HitBuffer[i].distance < best)
                {
                    best = HitBuffer[i].distance;
                    bestIndex = i;
                    bestTarget = target;
                }
            }
            if (bestIndex < 0) return false;

            if (bestTarget != null && bestTarget.IsAlive)
                bestTarget.TakeDamage(damage, HitBuffer[bestIndex].point);

            Despawn();
            return true;
        }

        private void Despawn()
        {
            inFlight = false;
            if (ownerPool != null) ownerPool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}

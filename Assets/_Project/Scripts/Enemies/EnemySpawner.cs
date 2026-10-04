using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARSurvival.Core;

namespace ARSurvival.Enemies
{
    [RequireComponent(typeof(EnemyFactory))]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("References (auto-found if empty)")]
        [SerializeField] private EnemyFactory factory;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Transform player;

        [Header("Spawn area (metres, around the arena centre)")]
        [SerializeField, Min(0.2f)] private float minSpawnRadius = 0.9f;
        [SerializeField, Min(0.3f)] private float maxSpawnRadius = 1.6f;
        [Tooltip("Never spawn closer than this to the player.")]
        [SerializeField, Min(0.2f)] private float minDistanceFromPlayer = 1.0f;
        [SerializeField, Min(1)] private int placementAttempts = 12;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float firstSpawnDelay = 1.5f;

        private GameManager game;
        private float nextSpawnTime;
        private bool spawning;

        private void Awake()
        {
            if (factory == null) factory = GetComponent<EnemyFactory>();
            if (planeManager == null) planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (player == null && Camera.main != null) player = Camera.main.transform;
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null) return;
            game.RoundStarted += BeginSpawning;
            game.RoundEnded += StopSpawning;
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.RoundStarted -= BeginSpawning;
            game.RoundEnded -= StopSpawning;
        }

        private void BeginSpawning()
        {
            factory.ReleaseAll();
            spawning = true;
            nextSpawnTime = Time.time + firstSpawnDelay;
        }

        private void StopSpawning() => spawning = false;

        private void Update()
        {
            if (!spawning || game == null || !game.IsPlaying) return;
            if (Time.time < nextSpawnTime) return;

            DifficultySettings d = game.Difficulty;
            nextSpawnTime = Time.time + (d != null ? d.SpawnInterval : 3f);

            int maxAlive = d != null ? d.MaxAliveEnemies : 6;
            if (factory.AliveCount >= maxAlive) return;

            SpawnOne(d);
        }

        private void SpawnOne(DifficultySettings d)
        {
            Transform arena = game.Arena;
            if (arena == null || player == null) return;

            Vector3 position = FindSpawnPoint(arena);
            float shooterChance = d != null ? d.ShooterChance : 0.35f;
            EnemyType type = Random.value < shooterChance ? EnemyType.Shooter : EnemyType.Melee;

            Vector3 toPlayer = player.position - position;
            toPlayer.y = 0f;
            Quaternion facing = toPlayer.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toPlayer.normalized, Vector3.up)
                : arena.rotation;

            factory.Create(type, position, facing, player, arena.position.y, d);
        }

        private Vector3 FindSpawnPoint(Transform arena)
        {
            Vector3 centre = arena.position;
            for (int i = 0; i < placementAttempts; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector3 candidate = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

                if (FlatDistance(candidate, player.position) < minDistanceFromPlayer) continue;
                if (IsOnDetectedPlane(candidate)) return candidate;
            }

            Vector3 away = centre - player.position;
            away.y = 0f;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : arena.forward;
            return centre + away * 0.4f;
        }

        private bool IsOnDetectedPlane(Vector3 worldPoint)
        {
            if (planeManager == null) return true;

            bool anyPlane = false;
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                anyPlane = true;
                if (Mathf.Abs(plane.transform.position.y - worldPoint.y) > 0.15f) continue;

                Vector3 local = plane.transform.InverseTransformPoint(worldPoint);
                if (PointInPolygon(new Vector2(local.x, local.z), plane.boundary)) return true;
            }
            return !anyPlane;
        }

        private static bool PointInPolygon(Vector2 p, NativeArray<Vector2> polygon)
        {
            if (!polygon.IsCreated || polygon.Length < 3) return false;

            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > p.y) != (b.y > p.y) &&
                               p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x;
                if (crosses) inside = !inside;
            }
            return inside;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}

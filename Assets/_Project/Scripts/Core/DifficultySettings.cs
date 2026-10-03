// =====================================================================
//  DifficultySettings.cs  —  Tunable gameplay values per difficulty
//
//  A ScriptableObject asset per mode (Easy, Hard). Gameplay systems read
//  these numbers instead of hard-coding them, so balancing happens in
//  the Inspector without touching code (bonus: difficulty levels).
// =====================================================================
using UnityEngine;

namespace ARSurvival.Core
{
    [CreateAssetMenu(fileName = "Difficulty", menuName = "AR Survival/Difficulty Settings")]
    public class DifficultySettings : ScriptableObject
    {
        [Header("Round")]
        [Tooltip("Shown on the menu button.")]
        [SerializeField] private string displayName = "Normal";
        [Tooltip("Seconds the player must survive.")]
        [SerializeField, Min(10f)] private float roundDuration = 90f;

        [Header("Player")]
        [SerializeField, Min(1)] private int playerMaxHealth = 100;

        [Header("Enemy spawning")]
        [Tooltip("Seconds between spawns.")]
        [SerializeField, Min(0.2f)] private float spawnInterval = 3f;
        [Tooltip("Maximum enemies alive at once.")]
        [SerializeField, Min(1)] private int maxAliveEnemies = 6;
        [Tooltip("Chance (0–1) that a spawned enemy is a Shooter instead of Melee.")]
        [SerializeField, Range(0f, 1f)] private float shooterChance = 0.35f;

        [Header("Enemy modifiers")]
        [SerializeField, Min(0.1f)] private float enemySpeedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float enemyDamageMultiplier = 1f;

        public string DisplayName => displayName;
        public float RoundDuration => roundDuration;
        public int PlayerMaxHealth => playerMaxHealth;
        public float SpawnInterval => spawnInterval;
        public int MaxAliveEnemies => maxAliveEnemies;
        public float ShooterChance => shooterChance;
        public float EnemySpeedMultiplier => enemySpeedMultiplier;
        public float EnemyDamageMultiplier => enemyDamageMultiplier;

#if UNITY_EDITOR
        /// <summary>Editor-only helper used by the setup tool to fill in values.</summary>
        public void EditorConfigure(string name, float duration, int health, float interval,
                                    int maxAlive, float shooter, float speed, float damage)
        {
            displayName = name; roundDuration = duration; playerMaxHealth = health;
            spawnInterval = interval; maxAliveEnemies = maxAlive; shooterChance = shooter;
            enemySpeedMultiplier = speed; enemyDamageMultiplier = damage;
        }
#endif
    }
}

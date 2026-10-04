using System;
using UnityEngine;

namespace ARSurvival.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "AR Survival/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Sound
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Random pitch +/- this amount so repeated sounds don't feel robotic.")]
            [Range(0f, 0.3f)] public float pitchVariation = 0.05f;

            public bool IsValid => clip != null;
            public float RandomPitch() => 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
        }

        [Header("Required gameplay sounds")]
        public Sound playerShoot = new Sound();
        public Sound playerDeath = new Sound();
        public Sound enemySpawn = new Sound();
        public Sound enemyShoot = new Sound();
        [Tooltip("Melee enemy attack hitting the player.")]
        public Sound meleeAttack = new Sound();

        [Header("Extra feedback")]
        public Sound enemyHit = new Sound();
        public Sound enemyDeath = new Sound();
        public Sound victory = new Sound();
        public Sound uiClick = new Sound();
        public Sound uiStart = new Sound();

        [Header("Environment")]
        public Sound ambientLoop = new Sound();
    }
}

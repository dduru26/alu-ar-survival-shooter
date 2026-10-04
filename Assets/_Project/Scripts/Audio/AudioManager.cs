using UnityEngine;
using UnityEngine.UI;
using ARSurvival.Core;
using ARSurvival.Enemies;
using ARSurvival.Player;

namespace ARSurvival.Audio
{
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SoundLibrary library;
        [Tooltip("How many positional (3D) sounds can play at the same time.")]
        [SerializeField, Range(2, 16)] private int spatialVoices = 8;
        [Tooltip("Metres. AR scale: the whole arena is only a few metres across.")]
        [SerializeField] private float spatialMinDistance = 0.5f;
        [SerializeField] private float spatialMaxDistance = 8f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.35f;

        [Header("References (auto-found if empty)")]
        [SerializeField] private PlayerShooter playerShooter;

        private AudioSource sfx2D;
        private AudioSource ambience;
        private AudioSource[] voices;
        private int nextVoice;
        private float ambienceTarget;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            sfx2D = CreateSource("SFX 2D", spatial: false);
            ambience = CreateSource("Ambience", spatial: false);
            ambience.loop = true;
            ambience.volume = 0f;

            voices = new AudioSource[spatialVoices];
            for (int i = 0; i < spatialVoices; i++) voices[i] = CreateSource($"Voice {i}", spatial: true);

            if (playerShooter == null) playerShooter = FindAnyObjectByType<PlayerShooter>();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.GameEnded += OnGameEnded;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.EnemyShot += OnEnemyShot;
            GameEvents.MeleeAttacked += OnMeleeAttacked;
            GameEvents.EnemyHit += OnEnemyHit;
            GameEvents.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.GameEnded -= OnGameEnded;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.EnemyShot -= OnEnemyShot;
            GameEvents.MeleeAttacked -= OnMeleeAttacked;
            GameEvents.EnemyHit -= OnEnemyHit;
            GameEvents.EnemyKilled -= OnEnemyKilled;
        }

        private void Start()
        {
            if (playerShooter != null) playerShooter.Fired += OnPlayerFired;
            HookButtons();
        }

        private void OnDestroy()
        {
            if (playerShooter != null) playerShooter.Fired -= OnPlayerFired;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Mathf.Approximately(ambience.volume, ambienceTarget)) return;
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceTarget, Time.unscaledDeltaTime * 0.5f);
            if (ambience.volume <= 0f && ambience.isPlaying) ambience.Stop();
        }

        private void OnPlayerFired() => Play2D(library.playerShoot);
        private void OnMeleeAttacked(Vector3 position) => Play2D(library.meleeAttack);
        private void OnEnemySpawned(EnemyType type, Vector3 position) => PlayAt(library.enemySpawn, position);
        private void OnEnemyShot(Vector3 position) => PlayAt(library.enemyShoot, position);
        private void OnEnemyHit(EnemyType type, Vector3 position) => PlayAt(library.enemyHit, position);
        private void OnEnemyKilled(EnemyType type, Vector3 position) => PlayAt(library.enemyDeath, position);

        private void OnGameEnded(GameSession session) =>
            Play2D(session != null && session.Survived ? library.victory : library.playerDeath);

        private void OnStateChanged(GameStateId state)
        {
            bool playing = state == GameStateId.Playing;
            ambienceTarget = playing && library.ambientLoop.IsValid ? ambienceVolume * library.ambientLoop.volume : 0f;
            if (playing && library.ambientLoop.IsValid && !ambience.isPlaying)
            {
                ambience.clip = library.ambientLoop.clip;
                ambience.Play();
            }
        }

        public void PlayUIClick() => Play2D(library.uiClick);
        public void PlayUIStart() => Play2D(library.uiStart);

        public void Play2D(SoundLibrary.Sound sound)
        {
            if (sound == null || !sound.IsValid) return;
            sfx2D.pitch = sound.RandomPitch();
            sfx2D.PlayOneShot(sound.clip, sound.volume);
        }

        public void PlayAt(SoundLibrary.Sound sound, Vector3 position)
        {
            if (sound == null || !sound.IsValid) return;
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;

            voice.transform.position = position;
            voice.clip = sound.clip;
            voice.volume = sound.volume;
            voice.pitch = sound.RandomPitch();
            voice.Play();
        }

        private AudioSource CreateSource(string sourceName, bool spatial)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = spatial ? 1f : 0f;
            if (spatial)
            {
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = spatialMinDistance;
                src.maxDistance = spatialMaxDistance;
                src.dopplerLevel = 0f;
            }
            return src;
        }

        private void HookButtons()
        {
            foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool isStart = b.name.StartsWith("StartButton") || b.name.StartsWith("RestartButton");
                b.onClick.AddListener(isStart ? PlayUIStart : PlayUIClick);
            }
        }
    }
}

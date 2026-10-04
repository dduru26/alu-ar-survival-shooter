using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ARSurvival.Combat;
using ARSurvival.Core;

namespace ARSurvival.Player
{
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [Tooltip("Seconds between shots while the screen is held.")]
        [SerializeField, Min(0.05f)] private float fireInterval = 0.18f;
        [Tooltip("Damage per bullet. Melee enemies have 2 HP, Shooters 4 HP.")]
        [SerializeField, Min(1)] private int damagePerShot = 1;
        [Tooltip("Where bullets appear, relative to the camera (right, down, forward).")]
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.05f, -0.07f, 0.18f);
        [Tooltip("Bullets converge on the crosshair at this distance (metres).")]
        [SerializeField, Min(0.5f)] private float aimDistance = 6f;

        public event Action Fired;
        public int ShotsFired { get; private set; }

        private float nextFireTime;
        private bool waitForRelease;
        private bool warnedNoPool;
        private GameManager game;

        private void Awake()
        {
            if (aimCamera == null) aimCamera = GetComponent<Camera>();
            if (aimCamera == null) aimCamera = Camera.main;
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game != null) game.RoundStarted += OnRoundStarted;
        }

        private void OnDestroy()
        {
            if (game != null) game.RoundStarted -= OnRoundStarted;
        }

        private void OnRoundStarted()
        {
            waitForRelease = true;
            ShotsFired = 0;
        }

        private void Update()
        {
            if (game == null || !game.IsPlaying) return;

            bool held = IsFireHeld();
            if (waitForRelease)
            {
                if (!held) waitForRelease = false;
                return;
            }

            if (held && Time.time >= nextFireTime) Fire();
        }

        private void Fire()
        {
            ProjectilePool pool = ProjectilePool.For(Team.Player);
            if (pool == null)
            {
                if (!warnedNoPool) Debug.LogWarning("[PlayerShooter] No Player ProjectilePool in the scene.", this);
                warnedNoPool = true;
                return;
            }

            Transform cam = aimCamera.transform;
            Vector3 origin = cam.TransformPoint(muzzleOffset);
            Vector3 aimPoint = cam.position + cam.forward * aimDistance;
            Vector3 direction = (aimPoint - origin).normalized;

            if (pool.Spawn(origin, direction, damagePerShot) == null) return;

            nextFireTime = Time.time + fireInterval;
            ShotsFired++;
            Fired?.Invoke();
        }

        private static bool IsFireHeld()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed) return false;

            if (Mouse.current != null && Mouse.current.rightButton.isPressed) return false;

            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }
    }
}

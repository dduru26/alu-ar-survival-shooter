using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARSurvival.AR
{
    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(ARPlaneManager))]
    [RequireComponent(typeof(ARAnchorManager))]
    public class ARPlacementController : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Prefab spawned on the first valid tap (the game arena).")]
        [SerializeField] private GameObject arenaPrefab;

        [Tooltip("If true, taps are accepted as soon as the scene starts. " +
                 "The GameManager will control this later (Placement state).")]
        [SerializeField] private bool acceptTapsOnStart = true;

        [Header("References (auto-found if empty)")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private Camera arCamera;

        public event Action<Transform> ArenaPlaced;

        public event Action PlacementReset;

        public bool IsPlaced => placedArena != null;
        public Transform PlacedArena => placedArena != null ? placedArena.transform : null;
        public bool AcceptingTaps { get; private set; }

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        private GameObject placedArena;
        private ARAnchor arenaAnchor;

        private void Awake()
        {
            if (raycastManager == null) raycastManager = GetComponent<ARRaycastManager>();
            if (planeManager == null)   planeManager   = GetComponent<ARPlaneManager>();
            if (anchorManager == null)  anchorManager  = GetComponent<ARAnchorManager>();
            if (arCamera == null)       arCamera       = Camera.main;

            AcceptingTaps = acceptTapsOnStart;
        }

        private void Update()
        {
            if (!AcceptingTaps || IsPlaced) return;
            if (!TryGetTapPosition(out Vector2 screenPos)) return;
            if (IsPointerOverUI()) return;

            if (raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
            {
                PlaceArena(Hits[0]);
            }
        }

        public void SetAcceptingTaps(bool accept) => AcceptingTaps = accept;

        public void ResetPlacement()
        {
            if (arenaAnchor != null) Destroy(arenaAnchor.gameObject);
            else if (placedArena != null) Destroy(placedArena);

            placedArena = null;
            arenaAnchor = null;
            PlacementReset?.Invoke();
        }

        private void PlaceArena(ARRaycastHit hit)
        {
            if (arenaPrefab == null)
            {
                Debug.LogError("[ARPlacementController] Arena Prefab is not assigned.", this);
                return;
            }

            Vector3 toCamera = arCamera.transform.position - hit.pose.position;
            toCamera.y = 0f;
            Quaternion rotation = toCamera.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : hit.pose.rotation;
            var pose = new Pose(hit.pose.position, rotation);

            ARPlane plane = planeManager.GetPlane(hit.trackableId);
            if (plane != null && anchorManager.enabled)
                arenaAnchor = anchorManager.AttachAnchor(plane, pose);

            if (arenaAnchor != null)
            {
                placedArena = Instantiate(arenaPrefab, arenaAnchor.transform);
                placedArena.transform.localPosition = Vector3.zero;
                placedArena.transform.localRotation = Quaternion.identity;
            }
            else
            {
                placedArena = Instantiate(arenaPrefab, pose.position, pose.rotation);
                arenaAnchor = placedArena.AddComponent<ARAnchor>();
            }

            placedArena.name = "Arena (placed)";
            ArenaPlaced?.Invoke(placedArena.transform);
        }

        private static bool TryGetTapPosition(out Vector2 position)
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                position = pointer.position.ReadValue();
                return true;
            }
            position = default;
            return false;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}

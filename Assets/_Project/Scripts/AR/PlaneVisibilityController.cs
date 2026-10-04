using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARSurvival.AR
{
    [RequireComponent(typeof(ARPlaneManager))]
    public class PlaneVisibilityController : MonoBehaviour
    {
        [Tooltip("Stop detecting new planes after the arena is placed (saves battery, cleaner view).")]
        [SerializeField] private bool stopDetectionAfterPlacement = true;

        [Tooltip("Hide the existing plane visuals after the arena is placed.")]
        [SerializeField] private bool hidePlanesAfterPlacement = true;

        [Header("References (auto-found if empty)")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARPlacementController placement;

        private void Awake()
        {
            if (planeManager == null) planeManager = GetComponent<ARPlaneManager>();
            if (placement == null)    placement    = GetComponent<ARPlacementController>();
        }

        private void OnEnable()
        {
            if (placement == null) return;
            placement.ArenaPlaced += HandleArenaPlaced;
            placement.PlacementReset += HandlePlacementReset;
        }

        private void OnDisable()
        {
            if (placement == null) return;
            placement.ArenaPlaced -= HandleArenaPlaced;
            placement.PlacementReset -= HandlePlacementReset;
        }

        private void HandleArenaPlaced(Transform arena)
        {
            if (stopDetectionAfterPlacement) planeManager.enabled = false;
            if (hidePlanesAfterPlacement) SetPlanesVisible(false);
        }

        private void HandlePlacementReset()
        {
            planeManager.enabled = true;
            SetPlanesVisible(true);
        }

        public void SetPlanesVisible(bool visible)
        {
            foreach (ARPlane plane in planeManager.trackables)
                plane.gameObject.SetActive(visible);
        }
    }
}

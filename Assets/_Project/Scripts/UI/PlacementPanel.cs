using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARSurvival.Core;
using ARSurvival.Data;

namespace ARSurvival.UI
{
    public class PlacementPanel : UIPanel
    {
        [SerializeField] private TMP_Text hint;
        [SerializeField] private Button backButton;
        [SerializeField] private ARPlaneManager planeManager;

        protected override void Awake()
        {
            base.Awake();
            if (planeManager == null) planeManager = FindAnyObjectByType<ARPlaneManager>();
            backButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
        }

        protected override void Update()
        {
            base.Update();
            if (!IsVisible || hint == null) return;
            bool found = planeManager != null && planeManager.trackables.count > 0;
            hint.text = found
                ? "Floor found!\n<b>Tap it</b> to place the arena"
                : "Move your phone slowly\nto scan the floor";
        }
    }
}

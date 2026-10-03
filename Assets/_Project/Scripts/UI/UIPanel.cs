// =====================================================================
//  UIPanel.cs  —  Abstract base for every screen
//
//  Inheritance: MainMenu, Placement, HUD, GameOver and Leaderboard panels
//  all extend this, sharing the fade in/out and show/hide logic.
//  Polymorphism: UIManager calls Show()/Hide() on any panel; each panel
//  overrides OnShow() to refresh its own content.
// =====================================================================
using UnityEngine;

namespace ARSurvival.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;

        protected CanvasGroup Group { get; private set; }
        public bool IsVisible { get; private set; }

        /// <summary>HUD-style panels return false so taps pass through to the game.</summary>
        protected virtual bool BlocksRaycasts => true;

        private float targetAlpha;

        protected virtual void Awake()
        {
            Group = GetComponent<CanvasGroup>();
            HideImmediate();
        }

        public void Show()
        {
            IsVisible = true;
            targetAlpha = 1f;
            Group.interactable = true;
            Group.blocksRaycasts = BlocksRaycasts;
            transform.SetAsLastSibling();
            OnShow();
        }

        public void Hide()
        {
            if (!IsVisible && targetAlpha == 0f) return;
            IsVisible = false;
            targetAlpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            OnHide();
        }

        public void HideImmediate()
        {
            if (Group == null) Group = GetComponent<CanvasGroup>();
            IsVisible = false;
            targetAlpha = 0f;
            Group.alpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;
        }

        protected virtual void Update()
        {
            if (Mathf.Approximately(Group.alpha, targetAlpha)) return;
            Group.alpha = Mathf.MoveTowards(Group.alpha, targetAlpha, Time.unscaledDeltaTime / fadeDuration);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}

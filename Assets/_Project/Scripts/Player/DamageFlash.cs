using UnityEngine;
using UnityEngine.UI;
using ARSurvival.Core;

namespace ARSurvival.Player
{
    [RequireComponent(typeof(Image))]
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.45f);
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.45f;

        private Image image;
        private float remaining;

        private void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            SetAlpha(0f);
        }

        private void OnEnable() => GameEvents.PlayerDamaged += Flash;
        private void OnDisable() => GameEvents.PlayerDamaged -= Flash;

        public void Flash() => remaining = fadeDuration;

        private void Update()
        {
            if (remaining <= 0f) return;
            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
            SetAlpha(flashColor.a * (remaining / fadeDuration));
        }

        private void SetAlpha(float a) =>
            image.color = new Color(flashColor.r, flashColor.g, flashColor.b, a);
    }
}

using UnityEngine;
using ARSurvival.Core;

namespace ARSurvival.Player
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ShowWhilePlaying : MonoBehaviour
    {
        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Apply(GameManager.Instance != null ? GameManager.Instance.State : GameStateId.Menu);
        }

        private void OnEnable() => GameEvents.StateChanged += Apply;
        private void OnDisable() => GameEvents.StateChanged -= Apply;

        private void Apply(GameStateId state) => group.alpha = state == GameStateId.Playing ? 1f : 0f;
    }
}

// =====================================================================
//  TrainingDummy.cs  —  TEMPORARY target for testing shooting (Phase 4)
//  Removed in Phase 5 when real enemies arrive.
//  Flashes white when hit; after N hits it "dies", awards a kill and resets.
// =====================================================================
using UnityEngine;
using ARSurvival.Core;

namespace ARSurvival.Combat
{
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int hitsToKill = 3;
        [SerializeField] private Color baseColor = new Color(0.85f, 0.85f, 0.85f);
        [SerializeField] private Color hitColor = Color.white;

        public Team Team => Team.Enemy;
        public bool IsAlive => true;

        private Renderer rend;
        private MaterialPropertyBlock block;
        private int hits;
        private float flashUntil;

        private void Awake()
        {
            rend = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            SetColor(baseColor);
        }

        public void TakeDamage(int amount, Vector3 hitPoint)
        {
            hits += amount;
            flashUntil = Time.time + 0.08f;
            SetColor(hitColor);
            if (hits >= hitsToKill)
            {
                hits = 0;
                GameManager.Instance?.RegisterKill(10);
                transform.localScale *= 1.25f;   // little "pop" so the kill is visible
            }
        }

        private void Update()
        {
            if (flashUntil > 0f && Time.time >= flashUntil)
            {
                flashUntil = 0f;
                SetColor(baseColor);
            }
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * 0.2f, Time.deltaTime * 6f);
        }

        private void SetColor(Color c)
        {
            if (rend == null) return;
            rend.GetPropertyBlock(block);
            block.SetColor("_BaseColor", c);
            rend.SetPropertyBlock(block);
        }
    }
}

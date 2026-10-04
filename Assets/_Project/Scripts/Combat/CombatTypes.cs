using UnityEngine;

namespace ARSurvival.Combat
{
    public enum Team { Player, Enemy }

    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }
        void TakeDamage(int amount, Vector3 hitPoint);
    }

    public interface IPoolable
    {
        void OnTakenFromPool();
        void OnReturnedToPool();
    }
}

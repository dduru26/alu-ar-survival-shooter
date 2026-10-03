// =====================================================================
//  CombatTypes.cs  —  Shared combat contracts
//
//  Abstraction: anything that can be shot (player, enemies, the test
//  dummy) implements IDamageable, so projectiles don't need to know
//  WHAT they hit — only that it can take damage and which team it's on.
// =====================================================================
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

    /// <summary>Callbacks for objects that live in an object pool.</summary>
    public interface IPoolable
    {
        /// <summary>Called right after the object is taken out of the pool.</summary>
        void OnTakenFromPool();
        /// <summary>Called right before the object goes back into the pool.</summary>
        void OnReturnedToPool();
    }
}

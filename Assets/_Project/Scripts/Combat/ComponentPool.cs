// =====================================================================
//  ComponentPool.cs  —  Generic Object Pool
//
//  PATTERN: Object Pooling (mandatory for projectiles).
//  • Pre-initialised: every instance is created up front (Prewarm),
//    before gameplay starts.
//  • No Instantiate/Destroy during gameplay: Get() re-activates a stored
//    instance, Release() deactivates it and puts it back.
//  • Reset: IPoolable callbacks let each object clear its own state.
//  • Fixed size by default (canGrow = false) so gameplay never allocates.
//
//  Generic (ComponentPool<T>) so the same code can pool bullets, enemies
//  or effects.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ARSurvival.Combat
{
    public class ComponentPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform container;
        private readonly bool canGrow;
        private readonly Stack<T> available = new Stack<T>();
        private readonly HashSet<T> active = new HashSet<T>();
        private readonly List<T> releaseBuffer = new List<T>();
        private int totalCreated;

        public int CountActive => active.Count;
        public int CountAvailable => available.Count;
        public int CountTotal => totalCreated;

        public ComponentPool(T prefab, int prewarmCount, Transform container, bool canGrow = false)
        {
            this.prefab = prefab;
            this.container = container;
            this.canGrow = canGrow;
            for (int i = 0; i < prewarmCount; i++) available.Push(CreateInstance());
        }

        /// <summary>Takes an object from the pool, or null if the pool is empty and can't grow.</summary>
        public T Get()
        {
            T item;
            if (available.Count > 0) item = available.Pop();
            else if (canGrow) item = CreateInstance();
            else return null;

            active.Add(item);
            item.gameObject.SetActive(true);
            (item as IPoolable)?.OnTakenFromPool();
            return item;
        }

        /// <summary>Returns an object to the pool. Ignores objects that aren't currently out.</summary>
        public void Release(T item)
        {
            if (item == null || !active.Remove(item)) return;
            (item as IPoolable)?.OnReturnedToPool();
            item.gameObject.SetActive(false);
            item.transform.SetParent(container, false);
            available.Push(item);
        }

        /// <summary>Returns every active object (used when a round ends).</summary>
        public void ReleaseAll()
        {
            releaseBuffer.Clear();
            releaseBuffer.AddRange(active);
            foreach (T item in releaseBuffer) Release(item);
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(prefab, container);
            instance.gameObject.SetActive(false);
            instance.name = $"{prefab.name}_{totalCreated:00}";
            totalCreated++;
            return instance;
        }
    }
}

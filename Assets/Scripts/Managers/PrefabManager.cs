﻿using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Scriptable;
using UnityEngine;

namespace Managers
{
    public class PrefabManager : Singleton<PrefabManager>
    {
        [SerializeField] private List<PrefabList> prefabLists;

        private readonly Dictionary<Prefabs, Prefab> _prefabs = new();
        private readonly Dictionary<Prefabs, Queue<GameObject>> _pools = new ();

        /// <summary>
        /// Static shortcut method for creating a prefab.
        /// </summary>
        /// <param name="prefab">The type of prefab to create.</param>
        /// <param name="parent">The parent transform.</param>
        /// <param name="setActive">The active state of the prefab.</param>
        public static GameObject Create(Prefabs prefab, Transform parent = null, bool setActive = true) =>
            Instance.Instantiate(prefab, parent, setActive);
        
        /// <summary>
        /// Overload for creating a prefab and returning a component.
        /// </summary>
        /// <param name="prefab">The type of prefab to create.</param>
        /// <param name="parent">The parent transform.</param>
        /// <param name="setActive">The active state of the prefab.</param>
        /// <typeparam name="T">The type of component to return.</typeparam>
        /// <returns>The component of the prefab.</returns>
        public static T Create<T>(Prefabs prefab, Transform parent = null, bool setActive = true) where T : Component
        {
            var newObject = Instance.Instantiate(prefab, parent, setActive);
            var component = newObject.GetComponent<T>();
            if (component == null)
                Debug.LogError($"Prefab {prefab} does not have component {typeof(T)}");
            
            return component;
        }
        
        /// <summary>
        /// Special singleton initializer method.
        /// </summary>
        public new static void Initialize()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/Managers/PrefabManager");
            if (prefab == null) throw new Exception("Missing PrefabManager prefab!");

            var instance = Instantiate(prefab);
            if (instance == null) throw new Exception("Failed to instantiate PrefabManager prefab!");

            instance.name = "Managers.PrefabManager (Singleton)";
        }

        protected override void OnAwake()
        {
            DontDestroyOnLoad(this);

            foreach (var prefab in prefabLists.SelectMany(prefabList => prefabList.prefabs))
            {
                _prefabs.Add(prefab.type, prefab);

                // If the prefab should be pooled, create a pool for it.
                if (!prefab.shouldPool) continue;
                var root = GameObject.Find(prefab.objectRoot);

                _pools.Add(prefab.type, new Queue<GameObject>());
                for (var i = 0; i < prefab.initialSize; i++)
                {
                    var newObject = Instantiate(prefab.prefab, root.transform);
                    newObject.SetActive(false);
                    _pools[prefab.type].Enqueue(newObject);
                }
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Creates a new instance of the prefab.
        /// </summary>
        /// <param name="prefab">The prefab type</param>
        /// <param name="parent">The parent transform.</param>
        /// <param name="setActive">The active state.</param>
        /// <returns>The created object.</returns>
        private GameObject Instantiate(Prefabs prefab, Transform parent = null, bool setActive = true)
        {
            var prefabData = _prefabs[prefab];

            GameObject newObject;
            if (prefabData.shouldPool)
            {
                var pool = _pools[prefab];

                if (!pool.Peek().activeSelf)
                {
                    // Use the object from the pool.
                    newObject = pool.Dequeue();
                    newObject.SetActive(true);

                    // Reset the transform.
                    newObject.transform.Reset(true, true);
                    // Call reset.
                    var poolObject = newObject.GetComponent<IPoolObject>();
                    poolObject?.Reset();
                }
                else
                {
                    // Create a new object.
                    newObject = Instantiate(prefabData.prefab);
                }

                // Re-add the object to the pool.
                pool.Enqueue(newObject);
            }
            else
            {
                newObject = Instantiate(prefabData.prefab, parent);
            }
            
            newObject.SetActive(setActive);

            if (prefabData.objectRoot == null) return newObject;

            var root = GameObject.Find(prefabData.objectRoot);
            if (root)
            {
                newObject.transform.SetParent(root.transform, false);
            }
            
            return newObject;
        }
    }

    public interface IPoolObject
    {
        /// <summary>
        /// Invoked when the object is reused.
        /// </summary>
        void Reset();
    }
}

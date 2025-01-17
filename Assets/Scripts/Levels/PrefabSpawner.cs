﻿using System;
using System.Collections.Generic;
using Managers;
using Objects.Scriptable;
using UnityEngine;

namespace Levels
{
    [Serializable]
    public struct SpawnData
    {
        public Transform spawnLocation;
        public Prefabs prefab;
    }
    
    public class PrefabSpawner : MonoBehaviour
    {
        [Tooltip("The dictionary of trigger location, spawn location, and prefabs.")]
        public UDictionary<Transform, SpawnData> prefabDictionary;
        
        private List<Transform> _triggersToRemove = new List<Transform>();
        
        private void Update()
        {
            if (prefabDictionary == null || GameManager.IsPaused) return;
            
            var playerPosition = GameManager.Instance.playerController.transform.position;
            
            foreach (var (triggerTransform, spawnData) in prefabDictionary)
            {
                if (Vector2.Distance(playerPosition, triggerTransform.position) <= 1.0f)
                {
                    PrefabManager.Create(spawnData.prefab, spawnData.spawnLocation);
                    _triggersToRemove.Add(triggerTransform);
                }
            }
            
            RemoveSpawn();
        }

        private void RemoveSpawn()
        {
            _triggersToRemove.ForEach(trigger => prefabDictionary.Remove(trigger));
            _triggersToRemove.Clear();
        }
    }
}
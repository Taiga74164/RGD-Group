﻿using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;

namespace Levels
{
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Platform Settings")]
        public List<Transform> waypoints;
        // public Transform targetWaypoint;
        public float speed = 1.0f;
        
        [Header("Platform State")]
        public List<Rigidbody2D> playersOnPlatform = new List<Rigidbody2D>();
        public Vector3 lastPosition;
        private Transform _transform;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private Vector3 _destination;
        
        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        
        private int _targetIndex;
        
        private void Start()
        {
            // Get the rigidbody component.
            _rb = GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            
            // Get the collider component.
            _collider = GetComponent<BoxCollider2D>();
            
            // Set the start position and target position for the platform.
            // _startPosition = transform.position;
            // _targetPosition = targetWaypoint.position;
            // _destination = _targetPosition;

            // Set the transform component and last position.
            _transform = transform;
            lastPosition = _transform.position;
        }

        private void Update()
        {
            if (GameManager.Instance.IsPaused) return;
            
            // var distance = Vector3.Distance(transform.position, _destination);
            // if (distance < 0.01f)
            // {
            //     _destination = _destination == _startPosition ? _targetPosition : _startPosition;
            // }
            
            // transform.position = Vector3.MoveTowards(transform.position, _destination, speed * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, waypoints[_targetIndex].position, speed * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.IsPaused) return;
            
            if (transform.position != waypoints[_targetIndex].position) return;
            _targetIndex = _targetIndex == waypoints.Count - 1 ? 0 : _targetIndex + 1;
        }
        
        private void LateUpdate()
        {
            if (GameManager.Instance.IsPaused) return;
            
            foreach (var playerRb in playersOnPlatform)
            {
                if (playerRb == null) continue;
                
                var velocity = _transform.position - lastPosition;
                playerRb.transform.Translate(velocity, _transform);
            }

            lastPosition = _transform.position;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.IsPlayer()) return;
            
            var playerRb = other.gameObject.GetComponent<Rigidbody2D>();
                
            var top = _collider.bounds.max.y;
            var bottom = other.collider.bounds.min.y;

            if (!(bottom > top)) return;
            
            AddPlayerToPlatform(playerRb);
                
            Debug.Log("Player is on platform.");
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (!other.IsPlayer()) return;
            
            var playerRb = other.gameObject.GetComponent<Rigidbody2D>();
            RemovePlayerFromPlatform(playerRb);
        }
        
        private void AddPlayerToPlatform(Rigidbody2D playerRb)
        {
            if (playersOnPlatform.Contains(playerRb)) return;
            playersOnPlatform.Add(playerRb);
        }
        
        private void RemovePlayerFromPlatform(Rigidbody2D playerRb)
        {
            if (!playersOnPlatform.Contains(playerRb)) return;
            playersOnPlatform.Remove(playerRb);
        }
    }
}
﻿using Enemies;
using Levels;
using UnityEngine;

namespace Weapons
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Piano : MonoBehaviour, IWeapon
    {
        [SerializeField] private float fallSpeed = 10.0f;
        public bool despawn;
        
        private Rigidbody2D _rigidbody2D;
        private bool _hasDropped;
        
        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _rigidbody2D.isKinematic = true;
        }

        private void Update()
        {
            if (!despawn && transform.position.y < -20f)
            {
                if (GetComponentInParent<Balloon>() != null)
                    GetComponentInParent<Balloon>().Respawn();
                
                Destroy(gameObject);
            }
            
            // Set the rigidbody to kinematic when the piano has stopped falling.
            // This prevents the piano from sinking through the ground when player is jumping on it.
            if (_hasDropped && _rigidbody2D.velocity.y >= 0.0f)
            {
                _rigidbody2D.velocity = Vector2.zero;
                _rigidbody2D.isKinematic = true;
                _hasDropped = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Enemy") && !despawn)
            {
                other.gameObject.GetComponent<Enemy>().GotHit(this);
                Destroy(gameObject);
            }
            else if (other.gameObject.GetComponent<IBreakable>() != null && !despawn)
            {
                Destroy(other.gameObject);
            }
            else if (!despawn)
            {
                Destroy(gameObject);
            }
        }

        public void DropPiano()
        {
            // Drop the piano.
            _hasDropped = true;
            _rigidbody2D.isKinematic = false;
            _rigidbody2D.velocity = new Vector2(0.0f, -fallSpeed);
        }
    }
}
using System.Collections.Generic;
using Managers;
using Objects.Scriptable;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapons;

namespace Controllers
{
    public class PieController : MonoBehaviour
    {
        [Header("Pie Settings")]
        [SerializeField] private float throwForce = 15.0f;
        [SerializeField] private float pieCooldownDuration = 1.0f;

        [Header("Trajectory Settings")]
        [SerializeField] private GameObject indicatorPrefab;
        [SerializeField] private float blockSize = 1.0f;
        [SerializeField] private AnimationCurve indicatorCurve;
        [SerializeField] private bool drawMaxDistance;
        
        private PlayerInput _playerInput;
        private Rigidbody2D _rb;
        private Camera _mainCamera;
        private Vector2 _screenResolution;
        private float _lastPieThrownTime = -1.0f;
        private List<GameObject> _indicators = new List<GameObject>();

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _rb = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;
            _screenResolution = new Vector2(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (GameManager.IsPaused) return;
            
            // Clear the trajectory if the pie is not ready.
            ClearTrajectory();
            
            // Draw the trajectory if the pie is ready.
            if (IsPieReady())
                DrawTrajectory();
        }

        private void DrawTrajectory()
        {
            // Get the mouse position in world space.
            var mousePosition = GetAimInput();
            // Calculate the direction and force of the throw.
            var direction = (mousePosition - transform.position).normalized;
            // Add the current velocity to the throw force.
            var velocity = (direction.normalized * throwForce).Add(_rb.velocity / 2);
            // Calculate the distance to the mouse. If drawMaxDistance is true, calculate the max distance.
            var distanceToMouse = drawMaxDistance ? CalculateMaxDistance(velocity) : 
                Vector2.Distance(mousePosition, transform.position);
            // Calculate the rounded total arrows to draw based on the distance to the mouse.
            var totalArrows = Mathf.FloorToInt(distanceToMouse / blockSize);
            
            for (var i = 0; i < totalArrows; i++)
            {
                // Calculate the time it takes for the pie to reach the next arrow position.
                var time = (i + 1) * blockSize / velocity.magnitude;
                // Calculate the trajectory point at the given time.
                var position = CalculateTrajectoryPoint(transform.position, velocity, time);
                // Calculate the rotation of the arrow based on the trajectory point.
                // var rotation = Quaternion.LookRotation(Vector3.forward, 
                //     CalculateTrajectoryPoint(transform.forward, force, time) - position);
                
                var indicator = Instantiate(indicatorPrefab, position, Quaternion.identity, transform);
                _indicators.Add(indicator);
                
                // Calculate the curve value based on the current arrow and the total arrows.
                var curveValue = indicatorCurve.Evaluate(i / (float)totalArrows);
                
                // Scale the arrow based on the curve value.
                indicator.transform.localScale *= curveValue;
                // Set the alpha of the arrow based on the curve value.
                var spriteRenderer = indicator.GetComponent<SpriteRenderer>();
                spriteRenderer.color = new Color(1, 1, 1, curveValue);
            }
        }
        
        /// <summary>
        /// Calculate the trajectory point based on the position, force, and time.
        /// Equation: f(t) = (x0 + x*t, y0 + y*t - g*t²/2)
        /// Where:
        /// - f(t) is the trajectory point at time t.
        /// - x0 and y0 are the initial position.
        /// - x and y are the velocity.
        /// - g is the gravity.
        /// - t is the time.
        /// </summary>
        /// <param name="position">The position of the object. Represents x0 and y0.</param>
        /// <param name="velocity">The velocity of the object. Represents x and y.</param>
        /// <param name="time">The time to calculate the trajectory point at. Represents t.</param>
        /// <returns>The calculated trajectory point at the given time.</returns>
        private static Vector2 CalculateTrajectoryPoint(Vector2 position, Vector2 velocity, float time) 
            => position + // x0, y0
               (velocity * time) + // x*t, y*t 
               Physics2D.gravity * Mathf.Pow(time, 2) / 2; // g*t²/2 
        
        /// <summary>
        /// Calculate the max distance of the trajectory.
        /// Equation: R = v^2 sin(2θ) / g
        /// Where:
        /// - R is the range.
        /// - v is the initial velocity.
        /// - theta is the angle of projection.
        /// - g is the gravity.
        /// </summary>
        /// <param name="initialVelocity">The initial velocity of the object.</param>
        /// <returns>The calculated max distance of the trajectory.</returns>
        private static float CalculateMaxDistance(Vector2 initialVelocity)
        {
            var g = Mathf.Abs(Physics2D.gravity.y);
            var angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x); // 45 * Mathf.Deg2Rad; 
            var distance = Mathf.Pow(initialVelocity.magnitude, 2) * Mathf.Sin(2 * angle) / g;
            return distance;
        }
        
        private void ClearTrajectory()
        {
            _indicators.ForEach(Destroy);
            _indicators.Clear();
        }

        public void HandlePieThrow()
        {
            if (!IsPieReady()) return;
            
            // Get the mouse position in world space.
            var mousePosition = GetAimInput();
            // Calculate the direction and force of the throw.
            var direction = (mousePosition - transform.position).normalized;
            // Add the current velocity to the throw force.
            var force = (direction.normalized * throwForce).Add(_rb.velocity / 2);
            
            // Create a new pie and throw it.
            var pie = PrefabManager.Create<Pie>(Prefabs.Pie, transform);
            pie.throwForce = force;
            pie.direction = direction;
            pie.ThrowPie();
            
            // Update the last time a pie was thrown.
            _lastPieThrownTime = Time.time;
        }

        private Vector3 GetAimInput()
        {
            var aimInput = InputManager.Aim.ReadValue<Vector2>();
            if (_playerInput.currentControlScheme.Equals("KBM"))
            {
                // Keyboard and mouse.
                return _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }
            else
            {
                // Gamepad.
                Debug.Log($"Aim Input: {aimInput.x}, {aimInput.y}");
                var width = Mathf.Clamp((aimInput.x + 1) / 2.0f * _screenResolution.x, 0f, 
                    _screenResolution.x);
                var height = Mathf.Clamp((aimInput.y + 1) / 2.0f * _screenResolution.y, 0f, 
                    _screenResolution.y);
                var aimPosition = new Vector3(width, height, _mainCamera.nearClipPlane);
                return _mainCamera.ScreenToWorldPoint(aimPosition);
            }
        }
        
        private bool IsPieReady() => !GameManager.IsPaused && Time.time - _lastPieThrownTime >= pieCooldownDuration;
    }
}
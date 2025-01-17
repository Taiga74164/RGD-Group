using Controllers;
using Managers;
using UnityEngine;

namespace Levels
{
    public class Pitfall : MonoBehaviour
    {
        public Transform respawnPoint;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.GetComponent<PlayerController>()) return;
            
            TransitionManager.Instance.RespawnTransition();
            var respawnPosition = respawnPoint.position;
            var newRespawn = new Vector2(respawnPosition.x, respawnPosition.y + 2.0f);
            other.transform.position = Vector2.Lerp(other.transform.position, newRespawn, 5.0f);
        }
    }
}

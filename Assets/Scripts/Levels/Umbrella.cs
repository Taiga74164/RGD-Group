using Controller;
using UnityEngine;

public class Umbrella : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            collision.GetComponent<PlayerController>().hasUmbrella = true;
            Destroy(gameObject);
        }
    }
}

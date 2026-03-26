using System.Runtime.CompilerServices;
using UnityEngine;

public class FallDetector : MonoBehaviour
{
   void OnTriggerEnter (Collider other)
    {
        if (other.CompareTag("Player"))
        {
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        if(player != null)
            {
                Debug.Log(other.name + " Fall Dead");
                player.TakeDamage(999999);

            }

        }
    }
}

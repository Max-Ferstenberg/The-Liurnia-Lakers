using UnityEngine;

public class KickTrigger : MonoBehaviour
{
    // Reference to the enemy that will perform the kick.
    public DoorKicker doorKicker; 

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            doorKicker.TriggerKick(other.gameObject);
        }
    }
    
    // Optional: Visualize the trigger area.
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if(col != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}

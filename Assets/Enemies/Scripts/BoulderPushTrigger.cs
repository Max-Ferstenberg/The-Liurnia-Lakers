using UnityEngine;

//Simple script that checks when the player enters range for a boulder trap

public class BoulderPushTrigger : MonoBehaviour
{
    public BoulderPusher boulderPusher;
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);
        if (other.CompareTag("Player"))
        {
            boulderPusher.TriggerPush();
        }
    }

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
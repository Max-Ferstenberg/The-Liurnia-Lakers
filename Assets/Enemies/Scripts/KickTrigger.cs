using UnityEngine;


//Just checks when the player enters an area to trigger the timings for one trap/interaction and calls function in other script
public class KickTrigger : MonoBehaviour
{
    public DoorKicker doorKicker; 

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            doorKicker.TriggerKick(other.gameObject);
        }
    }
}

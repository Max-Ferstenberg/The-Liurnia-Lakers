using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    private Elevator elevator;

    void Start()
    {
        elevator = GetComponentInParent<Elevator>();
    }

    void OnTriggerEnter(Collider other)
    {
        //Check that the player is the one on the elevator
        if (other.CompareTag("Player"))
        {
            //Notify parent elevator that player has entered, since the elevator itself uses it's own collider mesh and rigidbody, we hover a separate trigger just above the elevator
            elevator.PlayerEntered(other);
        }
    }

    //Same as above, but for leaving the elevator
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            elevator.PlayerExited(other);
        }
    }
}

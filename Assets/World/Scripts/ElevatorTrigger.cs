using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    private Elevator elevator;

    void Start()
    {
        elevator = GetComponentInParent<Elevator>();
        if (elevator == null)
            Debug.LogError("ElevatorTrigger: No ElevatorPlatform component found in parent.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            elevator.PlayerEntered(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            elevator.PlayerExited(other);
        }
    }
}

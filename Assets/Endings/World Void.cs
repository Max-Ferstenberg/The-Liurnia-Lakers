using UnityEngine;
using System.Collections;

public class WorldVoid : MonoBehaviour
{
    public EndingHandler endingHandler;
    public AudioSource gameSource;
    public AudioSource playerSource;
    public AudioClip deathSound;
    public AudioClip deathScream;
    public CameraController camera;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            camera.FallDeathCam();
            gameSource.PlayOneShot(deathSound);
            playerSource.PlayOneShot(deathScream);
            StartCoroutine(TriggerDeathAfterDelay(1f)); 
        }
    }

    private IEnumerator TriggerDeathAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (endingHandler != null)
        {
            endingHandler.Death();
        }
    }
}
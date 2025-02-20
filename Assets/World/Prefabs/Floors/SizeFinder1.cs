using UnityEngine;

public class SizeFinder1 : MonoBehaviour
{
    Renderer rend;

    void Awake()
    {
        rend = gameObject.GetComponent<Renderer>();
    }

    void Start(){
        if(rend != null)
        {
            Vector3 size = rend.bounds.size;
            Debug.Log("World Size: " + size);
        }
    }
}

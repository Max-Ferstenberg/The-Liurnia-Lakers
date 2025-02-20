using UnityEngine;

public class FireBreathDamage : MonoBehaviour
{
    [Tooltip("Damage to deal when the player enters the fire breath area.")]
    public float damage = 20f;

    [Tooltip("Layers for walls/obstacles that block fire breath damage.")]
    public LayerMask wallMask;

    private bool hasDealtDamage = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasDealtDamage && other.CompareTag("Player"))
        {
            Collider playerCol = other;
            if (playerCol != null)
            {
                Vector3 center = playerCol.bounds.center;
                Vector3 top = center + Vector3.up * playerCol.bounds.extents.y;
                Vector3 bottom = center - Vector3.up * playerCol.bounds.extents.y;
                
                if (IsClearPath(transform.position, center) ||
                    IsClearPath(transform.position, top) ||
                    IsClearPath(transform.position, bottom))
                {
                    PlayerMovement pm = other.GetComponent<PlayerMovement>();
                    if (pm != null)
                    {
                        pm.TakeDamageFromBoss(damage);
                        hasDealtDamage = true;
                        Debug.Log("FireBreathDamage applied " + damage + " damage.");
                    }
                }
                else
                {
                    Debug.Log("FireBreathDamage: Path blocked.");
                }
            }
        }
    }

    bool IsClearPath(Vector3 origin, Vector3 target)
    {
        Vector3 direction = (target - origin).normalized;
        float distance = Vector3.Distance(origin, target);
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, wallMask))
        {
            Debug.Log("Blocked by: " + hit.collider.name);
            return false;
        }
        return true;
    }

    public void ResetDamage()
    {
        hasDealtDamage = false;
    }
}
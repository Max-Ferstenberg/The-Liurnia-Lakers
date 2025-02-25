using UnityEngine;
using System.Collections.Generic;

public class NodeComponent : MonoBehaviour {
    public bool walkable = true;
    public List<NodeComponent> neighbours;
    
    [HideInInspector]
    public float gCost;
    [HideInInspector]
    public float hCost;
    public float fCost { get { return gCost + hCost; } }
    [HideInInspector]
    public NodeComponent parent;
}
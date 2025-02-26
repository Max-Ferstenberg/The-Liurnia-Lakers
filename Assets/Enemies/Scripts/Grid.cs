using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Node 
{
    public Vector3 worldPosition;
    public bool walkable;
    public float movementCost;
    public int gridX, gridY, gridZ;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Node parent;
}

public class Grid : MonoBehaviour {
    [ExecuteAlways]
    [Header("Grid Settings")]
    public Vector3 gridWorldSize; // The total size of your grid
    public float nodeDiameter;      // Half the size of each grid cell
    public LayerMask groundMask;  // Which layers count as walkable ground
    private Vector3 worldBottomLeft;
    public int GridSizeY => gridSizeY;
    public Node[,,] Nodes => _grid;
    
    Node[,,] _grid;

    public IEnumerable<Node> AllNodes {
        get {
            foreach(Node node in _grid) {
                yield return node;
            }
        }
    }

    [Header("Grid Size Parameters")]
    int gridSizeX, gridSizeY, gridSizeZ;

    [Header("Player Transform")]
    private Transform player;        // Add this field

    void Awake()
    {
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeDiameter);

        // Allocate memory for the grid array
        _grid = new Node[gridSizeX, gridSizeY, gridSizeZ];
        worldBottomLeft = transform.position - (gridWorldSize / 2);  
        CreateGrid();
    }


    public void Initialize(Transform player)
    {
        this.player = player;
    }
    
    void CreateGrid()
    {
        for(int x = 0; x < gridSizeX; x++)
        {
            for(int z = 0; z < gridSizeZ; z++)
            {
                Vector3 worldPoint = worldBottomLeft + 
                    Vector3.right * (x * nodeDiameter) +
                    Vector3.forward * (z * nodeDiameter);
                
                RaycastHit hit;
                if(Physics.Raycast(worldPoint + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
                {
                    float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                    float cost = Mathf.Lerp(1f, 5f, slopeAngle/45f); // 1=flat, 5=45° slope
                    
                    _grid[x,0,z] = new Node() {
                        worldPosition = hit.point,
                        walkable = slopeAngle <= 45f,
                        movementCost = cost
                    };
                }
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition) {
        // Calculate position relative to the grid's global origin (worldBottomLeft)
        Vector3 localPos = worldPosition - worldBottomLeft;
        
        // Convert to grid index
        int x = Mathf.FloorToInt(localPos.x / nodeDiameter);
        int y = Mathf.FloorToInt(localPos.y / nodeDiameter);
        int z = Mathf.FloorToInt(localPos.z / nodeDiameter);

        // Clamp to grid bounds
        x = Mathf.Clamp(x, 0, gridSizeX - 1);
        y = Mathf.Clamp(y, 0, gridSizeY - 1);
        z = Mathf.Clamp(z, 0, gridSizeZ - 1);

        return _grid[x, y, z];
    }

    public List<Node> GetNeighbours(Node node) {
         List<Node> neighbours = new List<Node>();
         for (int x = -1; x <= 1; x++){
            for (int y = -1; y <= 1; y++){
                for (int z = -1; z <= 1; z++){
                    if (x == 0 && y == 0 && z == 0) continue;
                    if (y != 0 && !IsVerticalConnectionValid(node, y)) continue;
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    int checkZ = node.gridZ + z;
                    if (checkX >= 0 && checkX < gridSizeX &&
                        checkY >= 0 && checkY < gridSizeY &&
                        checkZ >= 0 && checkZ < gridSizeZ) {
                            neighbours.Add(_grid[checkX, checkY, checkZ]);
                    }
                }
            }
         }
         return neighbours;
    }

    // Validate vertical movement
    private bool IsVerticalConnectionValid(Node node, int yOffset)
    {
        int targetY = node.gridY + yOffset;
        if (targetY < 0 || targetY >= gridSizeY) return false;

        Node verticalNode = _grid[node.gridX, targetY, node.gridZ];
        if (verticalNode == null || !verticalNode.walkable) return false;

        else return true;
    }

    public bool IsWalkablePath(Vector3 start, Vector3 end)
    {
        // Disallow paths through stairs unless elevation difference exists
        Node startNode = NodeFromWorldPoint(start);
        Node endNode = NodeFromWorldPoint(end);

        // Existing walkable check
        float step = nodeDiameter * 0.5f;
        Vector3 direction = (end - start).normalized;
        int steps = Mathf.CeilToInt(Vector3.Distance(start, end) / step);
        
        for (int i = 0; i <= steps; i++)
        {
            Vector3 checkPoint = start + direction * step * i;
            Node node = NodeFromWorldPoint(checkPoint);
            if (node == null || !node.walkable) return false;
        }
        return true;
    }

    void OnDrawGizmos() {
        // Set the gizmo color
        Gizmos.color = Color.white;
        // Draw a wireframe cube centered at the Grid's transform.position with size gridWorldSize
        Gizmos.DrawWireCube(transform.position, gridWorldSize);
 
    }
}
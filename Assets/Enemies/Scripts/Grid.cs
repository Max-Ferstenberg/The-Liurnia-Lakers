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
    private float elevationThreshold; // Add this field

    [Header("Player Transform")]
    private Transform player;        // Add this field

    void Awake()
    {
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeDiameter);

        CreateGrid();
        ProcessStairNeighbors();
    }

    public void Initialize(float elevationThreshold, Transform player)
    {
        this.elevationThreshold = elevationThreshold;
        this.player = player;
    }
    
    void CreateGrid()
    {
        _grid = new Node[gridSizeX, gridSizeY, gridSizeZ];
        worldBottomLeft = transform.position - 
            Vector3.right * gridWorldSize.x/2 - 
            Vector3.forward * gridWorldSize.z/2;

        // PHASE 1: Grid init
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Vector3 columnBase = worldBottomLeft + 
                    Vector3.right * (x * nodeDiameter) +
                    Vector3.forward * (z * nodeDiameter);

                // Smaller raycast increments for stair detection
                float castStartHeight = 10f;
                float castStep = nodeDiameter * 0.1f;
                List<Vector3> stairSteps = new List<Vector3>();

                for (float y = castStartHeight; y > -castStartHeight; y -= castStep)
                {
                    Vector3 rayStart = columnBase + Vector3.up * y;
                    RaycastHit hit;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, groundMask))
                    {
                        if (hit.collider.CompareTag("Stairs"))
                        {
                            Vector3 snappedPos = new Vector3(
                                columnBase.x,
                                hit.point.y,
                                columnBase.z
                            );
                            
                            if (!stairSteps.Any(step => Mathf.Abs(step.y - snappedPos.y) < nodeDiameter/2))
                            {
                                stairSteps.Add(snappedPos);
                            }
                        }
                    }
                }

                foreach (Vector3 stepPos in stairSteps)
                {
                    int y = Mathf.FloorToInt((stepPos.y - worldBottomLeft.y) / nodeDiameter);
                    y = Mathf.Clamp(y, 0, gridSizeY - 1);

                    if (_grid[x, y, z] == null)
                    {
                        _grid[x, y, z] = new Node()
                        {
                            worldPosition = stepPos,
                            walkable = true,
                            movementCost = 3f,
                            gridX = x,
                            gridY = y,
                            gridZ = z
                        };
                    }
                }

                // Fill remaining Y levels
                for (int y = 0; y < gridSizeY; y++)
                {
                    if (_grid[x, y, z] == null)
                    {
                        _grid[x, y, z] = new Node()
                        {
                            worldPosition = columnBase + Vector3.up * (y * nodeDiameter),
                            walkable = true,
                            movementCost = 1f,
                            gridX = x,
                            gridY = y,
                            gridZ = z
                        };
                    }
                }
            } // End z loop
        } // End x loop

        // PHASE 2: Stair connection system
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Node currentNode = _grid[x, y, z];
                    if (currentNode == null || currentNode.movementCost != 3f) continue;

                    if (y > 0)
                    {
                        if (_grid[x, y-1, z] != null)
                        {
                            _grid[x, y-1, z].movementCost = 3f;
                            _grid[x, y-1, z].walkable = true;
                        }

                        if (x > 0 && _grid[x-1, y-1, z] != null)
                        {
                            _grid[x-1, y-1, z].movementCost = 3f;
                            _grid[x-1, y-1, z].walkable = true;
                        }

                        if (x < gridSizeX-1 && _grid[x+1, y-1, z] != null)
                        {
                            _grid[x+1, y-1, z].movementCost = 3f;
                            _grid[x+1, y-1, z].walkable = true;
                        }
                    }
                }
            }
        }

        // PHASE 3: Final validation pass
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Node node = _grid[x, y, z];
                    if (node == null)
                    {
                        _grid[x, y, z] = new Node()
                        {
                            worldPosition = worldBottomLeft + 
                                Vector3.right * (x * nodeDiameter + nodeDiameter) +
                                Vector3.up * (y * nodeDiameter + nodeDiameter) +
                                Vector3.forward * (z * nodeDiameter + nodeDiameter),
                            walkable = false,
                            movementCost = Mathf.Infinity,
                            gridX = x,
                            gridY = y,
                            gridZ = z
                        };
                    }
                }
            }
        }
    } // End CreateGrid()

    void ProcessStairNeighbors()
    {
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Node node = _grid[x, y, z];
                    if (node.movementCost == 3f)
                    {
                        // Connect to adjacent flat nodes at the same Y level
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;
                                
                                int checkX = x + dx;
                                int checkZ = z + dz;
                                int checkY = y; // Same Y level

                                if (checkX >= 0 && checkX < gridSizeX &&
                                    checkZ >= 0 && checkZ < gridSizeZ)
                                {
                                    Node neighbor = _grid[checkX, checkY, checkZ];
                                    if (neighbor != null && neighbor.movementCost == 1f)
                                    {
                                        neighbor.movementCost = 3f; // Mark as stair entry
                                        neighbor.walkable = true;
                                    }
                                }
                            }
                        }
                    }
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

    // In Grid.cs
    public Node FindStairEntrypoint(Vector3 enemyPosition)
    {
        Node enemyNode = NodeFromWorldPoint(enemyPosition);
        
        // Search horizontally for the nearest stair node at the same Y level
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Node node = _grid[x, enemyNode.gridY, z];
                if (node != null && node.movementCost == 3f)
                {
                    return node;
                }
            }
        }
        
        // Fallback: Use existing stair endpoint logic
        return FindStairEndpoint(enemyNode, false);
    }

    public Node FindStairEndpoint(Node stairNode, bool findTop)
    {
        if (stairNode.movementCost != 3f) return stairNode;

        int x = stairNode.gridX;
        int z = stairNode.gridZ;
        int y = stairNode.gridY;

        if (findTop)
        {
            for (int i = y; i < gridSizeY; i++)
            {
                Node node = _grid[x, i, z];
                // Stop at first non-stair node or grid boundary
                if (node == null || node.movementCost != 3f) 
                    return _grid[x, Mathf.Min(i-1, gridSizeY-1), z];
            }
        }
        else
        {
            for (int i = y; i >= 0; i--)
            {
                Node node = _grid[x, i, z];
                if (node == null || node.movementCost != 3f) 
                    return _grid[x, Mathf.Max(i+1, 0), z];
            }
        }
        return stairNode;
    }

    // Validate vertical movement
    private bool IsVerticalConnectionValid(Node node, int yOffset)
    {
        int targetY = node.gridY + yOffset;
        if (targetY < 0 || targetY >= gridSizeY) return false;

        Node verticalNode = _grid[node.gridX, targetY, node.gridZ];
        if (verticalNode == null || !verticalNode.walkable) return false;

        // Allow vertical movement if either node is a stair
        return node.movementCost == 3f || verticalNode.movementCost == 3f;
    }

    public bool IsWalkablePath(Vector3 start, Vector3 end)
    {
        // Disallow paths through stairs unless elevation difference exists
        Node startNode = NodeFromWorldPoint(start);
        Node endNode = NodeFromWorldPoint(end);
        float elevationDiff = Mathf.Abs(start.y - end.y);

        // Block stairs if no elevation difference AND player isn't on stairs
        bool playerOnStairs = NodeFromWorldPoint(player.position).movementCost == 3f;
        if (!playerOnStairs && elevationDiff < elevationThreshold && 
            (startNode.movementCost == 3f || endNode.movementCost == 3f))
        {
            return false;
        }

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

    private bool HasValidStairPath(Vector3 start, Vector3 end)
    {
        Node startNode = NodeFromWorldPoint(start);
        Node endNode = NodeFromWorldPoint(end);
        return startNode.movementCost == 3f || endNode.movementCost == 3f;
    }

    void OnDrawGizmos() {
        // Set the gizmo color
        Gizmos.color = Color.white;
        // Draw a wireframe cube centered at the Grid's transform.position with size gridWorldSize
        Gizmos.DrawWireCube(transform.position, gridWorldSize);
 
    }
}

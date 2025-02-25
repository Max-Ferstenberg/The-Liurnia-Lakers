using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AStarPathfinder {
    public static List<Node> FindPath(Grid grid, Vector3 startPos, Vector3 targetPos) {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        // Modified validation with stair awareness
        bool startValid = startNode != null && (startNode.walkable || startNode.movementCost == 3f);
        bool targetValid = targetNode != null && (targetNode.walkable || targetNode.movementCost == 3f);
        
        if (!startValid || !targetValid) {
            Debug.LogError("Invalid start/target node. StartValid: " + startValid + " TargetValid: " + targetValid);
            return null;
        }

        // Validate nodes
        if (startNode == null || targetNode == null || !startNode.walkable || !targetNode.walkable) {
            Debug.LogError("Invalid start/target node.");
            return new List<Node>(); // Return empty list instead of null
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while(openSet.Count > 0){
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++){
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) {
                    currentNode = openSet[i];
                }
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if(currentNode == targetNode){
                return RetracePath(startNode, targetNode, grid);
            }

            foreach(Node neighbour in grid.GetNeighbours(currentNode)){
                if (!neighbour.walkable || closedSet.Contains(neighbour)) continue;

                // Use movementCost instead of distance for path cost
                float newMovementCostToNeighbour = currentNode.gCost + neighbour.movementCost;

                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)){
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode); // Heuristic remains distance-based
                    neighbour.parent = currentNode;

                    if(!openSet.Contains(neighbour)){
                        openSet.Add(neighbour);
                    }
                }
            }
        }
        return null;
    }

    // In AStarPathfinder.cs
    static List<Node> RetracePath(Node startNode, Node endNode, Grid grid)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        if (startNode == null || endNode == null) return path;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
            
            // Prevent infinite loops
            if (path.Count > 1000)
            {
                Debug.LogError("Path retrace overflow");
                break;
            }
        }
        path.Reverse();
        bool allSameLevel = path.All(n => n.gridY == startNode.gridY);
        if(!allSameLevel)
        {
            // First pass: Simplify path while preserving stairs and ledges
            for (int i = 0; i < path.Count - 2; i++)
            {
                Node current = path[i];
                Node nextNext = path[i + 2];

                bool isStairNode = current.movementCost == 3f || 
                                path[i + 1].movementCost == 3f || 
                                nextNext.movementCost == 3f;

                bool isNearLedge = 
                    Mathf.Abs(current.worldPosition.y - nextNext.worldPosition.y) > 0.1f ||
                    !grid.IsWalkablePath(current.worldPosition, nextNext.worldPosition);

                // Never simplify paths involving stairs or ledges
                if (!isStairNode && !isNearLedge && 
                    Mathf.Abs(current.gridY - nextNext.gridY) == 0 && 
                    current.gridY == path[i + 1].gridY)
                {
                    path.RemoveAt(i + 1);
                    i--;
                }
            }

        }

        // Second pass: Enhance vertical movement interpolation (original code)
        for (int i = 0; i < path.Count - 2; i++)
        {
            Node a = path[i];
            Node c = path[i + 2];

            // Vertical alignment check with stair awareness
            if (Mathf.Abs(a.gridY - c.gridY) > 0)
            {
                // Find best intermediate node for smooth vertical transition
                Node intermediate = grid.GetNeighbours(a)
                    .Find(n => n.gridY == c.gridY && 
                        Mathf.Abs(n.worldPosition.y - c.worldPosition.y) < 0.1f && 
                        n.movementCost == 3f);

                if (intermediate != null && intermediate.walkable)
                {
                    // Insert stair transition node if needed
                    if (!path.Contains(intermediate))
                    {
                        path.Insert(i + 1, intermediate);
                        i--; // Re-process current index after insertion
                    }
                }
            }
        }

        // Third pass: Final simplification with vertical preservation (original code)
        for (int i = 0; i < path.Count - 2; i++)
        {
            Node a = path[i];
            Node c = path[i + 2];
            
            // Only remove if nodes are horizontally close AND on same level
            if (Mathf.Abs(a.gridY - c.gridY) == 0 &&
                Vector3.Distance(a.worldPosition, c.worldPosition) < grid.nodeDiameter * 1.5f)
            {
                path.RemoveAt(i + 1);
                i--; // Re-process current index after removal
            }
        }

        // Final check: Ensure stair transitions are maintained (original code)
        for (int i = 0; i < path.Count - 1; i++)
        {
            Node current = path[i];
            Node next = path[i + 1];
            
            // Add intermediate node if vertical jump too large
            if (Mathf.Abs(current.gridY - next.gridY) > 1 &&
                current.movementCost != 3f && next.movementCost != 3f)
            {
                Node stairNode = grid.GetNeighbours(current)
                    .FirstOrDefault(n => n.movementCost == 3f && 
                                    Mathf.Abs(n.gridY - next.gridY) <= 1);
                
                if (stairNode != null)
                {
                    path.Insert(i + 1, stairNode);
                    i--; // Re-process current index after insertion
                }
            }
        }

        return path;
    }

    static float GetDistance(Node nodeA, Node nodeB) 
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);

        // Reduced penalty if either node is a stair
        float verticalPenalty = (nodeA.movementCost == 3f || nodeB.movementCost == 3f) 
            ? 5f * dstY 
            : 1000f * dstY;

        return (dstX + dstZ) * 10 + verticalPenalty;
    }
}
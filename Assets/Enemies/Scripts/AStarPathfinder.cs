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
        // Basic path reversal without stair handling
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        
        while(currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    // Modify GetDistance() heuristic to allow vertical movement when stairs are involved
    static float GetDistance(Node nodeA, Node nodeB)
    {
        // Standard 3D distance without stair special cases
        return Vector3.Distance(nodeA.worldPosition, nodeB.worldPosition) * 10f;
    }
}
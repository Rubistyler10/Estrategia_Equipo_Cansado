
using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinding : MonoBehaviour
{
    public GridManager gridManager;

    public List<GridNode> FindPath(
        GridNode startNode,
        GridNode targetNode)
    {
        List<GridNode> openList = new List<GridNode>();

        HashSet<GridNode> closedList = new HashSet<GridNode>();

        if (startNode == null || targetNode == null ||
            !startNode.isWalkable || !targetNode.isWalkable)
        {
            return new List<GridNode>();
        }

        // Reiniciamos los datos de búsquedas anteriores.
        foreach (GridNode node in gridManager.Grid)
        {
            node.gCost = float.PositiveInfinity;
            node.hCost = 0f;
            node.parent = null;
        }

        startNode.gCost = 0f;
        startNode.hCost = GetHeuristic(startNode, targetNode);

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // Elegimos el nodo con el menor coste F.
            GridNode currentNode = openList[0];

            foreach (GridNode node in openList)
            {
                if (node.fCost < currentNode.fCost ||
                    (node.fCost == currentNode.fCost &&
                     node.hCost < currentNode.hCost))
                {
                    currentNode = node;
                }
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // Hemos llegado al destino.
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // Exploramos los vecinos.
            foreach (GridNode neighbor in currentNode.neighbors)
            {
                if (!neighbor.isWalkable ||
                    closedList.Contains(neighbor))
                {
                    continue;
                }

                float newCost = currentNode.gCost +
                    gridManager.GetMovementCost(
                        currentNode, neighbor
                    );

                if (newCost < neighbor.gCost)
                {
                    neighbor.gCost = newCost;
                    neighbor.hCost = GetHeuristic(
                        neighbor, targetNode
                    );

                    neighbor.parent = currentNode;

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        // No se ha encontrado ningún camino.
        return new List<GridNode>();
    }

    private float GetHeuristic(
        GridNode node, GridNode target)
    {
        int dx = Mathf.Abs(node.gridX - target.gridX);
        int dz = Mathf.Abs(node.gridZ - target.gridZ);

        int diagonal = Mathf.Min(dx, dz);
        int straight = Mathf.Max(dx, dz) - diagonal;

        return gridManager.cellSize *
            (diagonal * Mathf.Sqrt(2f) + straight);
    }

    private List<GridNode> RetracePath(
        GridNode startNode, GridNode targetNode)
    {
        List<GridNode> path = new List<GridNode>();

        GridNode currentNode = targetNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Add(startNode);
        path.Reverse();

        return path;
    }
}

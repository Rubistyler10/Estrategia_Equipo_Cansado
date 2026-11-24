
using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinding : MonoBehaviour
{
    public GridManager gridManager;

    public List<GridNode> FindPath(
    GridNode startNode,
    GridNode targetNode,
    AnimalUnit animal)
    {
        if (gridManager == null || gridManager.Grid == null ||
            startNode == null || targetNode == null ||
            !startNode.isWalkable || !targetNode.isWalkable)
        {
            return new List<GridNode>();
        }

        List<GridNode> openList = new List<GridNode>();
        HashSet<GridNode> closedList = new HashSet<GridNode>();

        // Reiniciamos los datos de búsquedas anteriores.
        foreach (GridNode node in gridManager.Grid)
        {
            node.gCost = float.PositiveInfinity;
            node.hCost = 0f;
            node.parent = null;
        }

        startNode.gCost = 0f;
        startNode.hCost = GetHeuristic(startNode, targetNode, animal);

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
                if (IsBlocked(neighbor, startNode) ||
                    closedList.Contains(neighbor))
                {
                    continue;
                }

                if (IsDiagonalMove(currentNode, neighbor) &&
                    HasBlockedCorner(currentNode, neighbor, startNode))
                {
                    continue;
                }

                float stepCost = GetStepCost(
                    currentNode, neighbor, animal
                );
                if (stepCost == float.PositiveInfinity)
                    continue;

                float newCost = currentNode.gCost + stepCost;

                if (newCost < neighbor.gCost)
                {
                    neighbor.gCost = newCost;
                    neighbor.hCost = GetHeuristic(
                        neighbor, targetNode, animal
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

    private bool IsBlocked(GridNode node, GridNode startNode)
    {
        return !node.isWalkable ||
               (node.isOccupied && node != startNode);
    }

    private bool IsDiagonalMove(GridNode from, GridNode to)
    {
        return from.gridX != to.gridX &&
               from.gridZ != to.gridZ;
    }

    private bool HasBlockedCorner(
        GridNode from, GridNode to, GridNode startNode)
    {
        GridNode horizontal =
            gridManager.Grid[to.gridX, from.gridZ];
        GridNode vertical =
            gridManager.Grid[from.gridX, to.gridZ];

        return IsBlocked(horizontal, startNode) ||
               IsBlocked(vertical, startNode);
    }

    private float GetStepCost(
        GridNode from, GridNode destination, AnimalUnit animal)
    {
        if (animal == null)
            return gridManager.GetMovementCost(from, destination);

        int terrainCost = animal.GetTerrainCost(destination.terrainType);
        return terrainCost == int.MaxValue
            ? float.PositiveInfinity
            : terrainCost;
    }

    private float GetHeuristic(
        GridNode node, GridNode target, AnimalUnit animal)
    {
        int dx = Mathf.Abs(node.gridX - target.gridX);
        int dz = Mathf.Abs(node.gridZ - target.gridZ);

        if (animal != null)
        {
            int minimumTerrainCost = Mathf.Min(
                animal.GetTerrainCost(TerrainType.Plain),
                animal.GetTerrainCost(TerrainType.Desert),
                animal.GetTerrainCost(TerrainType.Snow),
                animal.GetTerrainCost(TerrainType.Water)
            );

            if (minimumTerrainCost == int.MaxValue)
                return 0f;

            return Mathf.Max(dx, dz) * minimumTerrainCost;
        }

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

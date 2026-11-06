
using UnityEngine;
using System.Collections.Generic;

public class GridNode
{
    public Vector3 worldPosition;
    public bool isWalkable;

    public int gridX;
    public int gridZ;

    public List<GridNode> neighbors = new List<GridNode>();

    // Valores utilizados por A*
    public float gCost;
    public float hCost;

    // Nodo desde el que hemos llegado.
    public GridNode parent;

    // Coste total estimado.
    public float fCost
    {
        get { return gCost + hCost; }
    }

    public GridNode(Vector3 position, bool walkable, int x, int z)
    {
        worldPosition = position;
        isWalkable = walkable;

        gridX = x;
        gridZ = z;

        gCost = float.PositiveInfinity;
        hCost = 0f;
        parent = null;
    }
}

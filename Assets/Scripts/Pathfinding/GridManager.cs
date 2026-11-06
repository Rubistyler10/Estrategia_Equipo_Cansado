

using UnityEngine;


public class GridManager : MonoBehaviour
{
    public Transform ground;

    public int width = 80;
    public int depth = 80;
    public float cellSize = 0.5f;
    public LayerMask obstacleMask; // CON ESTO PODEMOS ELEGIR QUE CAPAS COMPROBAR

    private GridNode[,] grid;
    
    //para permitir que otros scripts accedan a la matriz
    public GridNode[,] Grid
    {
        get { return grid; }
    }

    private void Awake()
    {
        CreateGrid();
        CreateGraph();
    }

    private void CreateGrid()
    {
        if (ground == null || width <= 0 ||
            depth <= 0 || cellSize <= 0f)
        {
            Debug.LogError("Configuración del Grid incorrecta.");
            return;
        }

        grid = new GridNode[width, depth];

        float startX = ground.position.x - width * cellSize / 2f;
        float startZ = ground.position.z - depth * cellSize / 2f;
        float y = ground.position.y + 0.05f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3 position = new Vector3(
                    startX + (x + 0.5f) * cellSize,
                    y,
                    startZ + (z + 0.5f) * cellSize
                );


                //check box encima de las celdas y creación de nodo

                Vector3 checkPosition = new Vector3(
                    position.x,
                    ground.position.y + 0.5f,
                    position.z
                );

                Vector3 halfExtents = new Vector3(
                    cellSize * 0.48f,
                    0.45f,
                    cellSize * 0.48f
                );

                bool isWalkable = !Physics.CheckBox(
                    checkPosition,
                    halfExtents,
                    Quaternion.identity,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore //evitar que los colliders marcados como triggers cuenten como obstáculos
                );

                grid[x, z] = new GridNode(
                    position,
                    isWalkable,
                    x,
                    z
                );

            }
        }

        Debug.Log("Nodos creados: " + grid.Length);
    }



    private void CreateGraph()
    {
        if (grid == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                GridNode node = grid[x, z];

                node.neighbors.Clear();

                //filtro para los obstáculos
                if (!node.isWalkable)
                    continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        //cordenadas de el mismo 
                        if (dx == 0 && dz == 0)
                            continue;
                        //cordenadas del vecino
                        int nx = x + dx;
                        int nz = z + dz;

                        //evitar el fuera de rango
                        if (nx < 0 || nx >= width ||
                            nz < 0 || nz >= depth)
                            continue;

                        GridNode neighbor = grid[nx, nz];

                        if (!neighbor.isWalkable)
                            continue;

                        // vecinos en diagonal, evitar los problemas con las esquinas de los muros
                        if (dx != 0 && dz != 0)
                        {
                            if (!grid[x + dx, z].isWalkable ||
                                !grid[x, z + dz].isWalkable)
                                continue;
                        }

                        node.neighbors.Add(neighbor);
                    }
                }
            }
        }
    }

    public float GetMovementCost(GridNode from, GridNode to)
    {
        //calculamos la distancia entre posiciones 
        return Vector3.Distance(
            from.worldPosition,
            to.worldPosition
        );
    }


    //esto es para una prueba visual, después podemos quitarlo
    public GridNode GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        if (grid == null || ground == null)
            return null;

        float startX = ground.position.x
            - width * cellSize / 2f;

        float startZ = ground.position.z
            - depth * cellSize / 2f;

        int x = Mathf.FloorToInt(
            (worldPosition.x - startX) / cellSize
        );

        int z = Mathf.FloorToInt(
            (worldPosition.z - startZ) / cellSize
        );

        x = Mathf.Clamp(x, 0, width - 1);
        z = Mathf.Clamp(z, 0, depth - 1);

        return grid[x, z];
    }



    private void OnDrawGizmosSelected()
    {
        if (ground == null || cellSize <= 0f)
            return;

        Gizmos.color = Color.green;

        float startX = ground.position.x - width * cellSize / 2f;
        float startZ = ground.position.z - depth * cellSize / 2f;
        float y = ground.position.y + 0.05f;

        for (int x = 0; x <= width; x++)
        {
            float posX = startX + x * cellSize;

            Gizmos.DrawLine(
                new Vector3(posX, y, startZ),
                new Vector3(posX, y, startZ + depth * cellSize)
            );
        }

        for (int z = 0; z <= depth; z++)
        {
            float posZ = startZ + z * cellSize;

            Gizmos.DrawLine(
                new Vector3(startX, y, posZ),
                new Vector3(startX + width * cellSize, y, posZ)
            );
        }


        if (grid != null)
        {
            Gizmos.color = Color.red;

            foreach (GridNode node in grid)
            {
                if (!node.isWalkable)
                {
                    Gizmos.DrawCube(
                        node.worldPosition + Vector3.up * 0.02f,
                        new Vector3(
                            cellSize * 0.9f,
                            0.025f,
                            cellSize * 0.9f
                        )
                    );
                }
            }
        }

    }

}

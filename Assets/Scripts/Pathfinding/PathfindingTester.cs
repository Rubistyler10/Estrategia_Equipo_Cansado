
using UnityEngine;
using System.Collections.Generic;

public class PathfindingTester : MonoBehaviour
{
    public GridManager gridManager;
    public AStarPathfinding pathfinding;

    public Transform startPoint;
    public Transform targetPoint;

    private List<GridNode> path;

    private void Start()
    {
        Debug.LogWarning("PATHFINDING TESTER INICIADO");

        TestPath();
    }

    private void TestPath()
    {
        // Comprobamos que las referencias estén asignadas.
        if (gridManager == null || pathfinding == null ||
            startPoint == null || targetPoint == null)
        {
            Debug.LogError("Faltan referencias.");
            return;
        }

        // Convertimos las posiciones en nodos.
        GridNode startNode =
            gridManager.GetNodeFromWorldPosition(
                startPoint.position
            );

        GridNode targetNode =
            gridManager.GetNodeFromWorldPosition(
                targetPoint.position
            );

        // Comprobamos que los nodos sean válidos.
        if (startNode == null || targetNode == null)
        {
            Debug.LogError("No se han encontrado los nodos.");
            return;
        }

        // Calculamos el camino utilizando A*.
        path = pathfinding.FindPath(
            startNode,
            targetNode
        );

        // Mostramos el resultado.
        if (path.Count > 0)
        {
            Debug.LogWarning(
                "Camino encontrado. Nodos: " + path.Count
            );
        }
        else
        {
            Debug.LogWarning(
                "No se ha encontrado un camino."
            );
        }
    }

    private void OnDrawGizmos()
    {
        // Si no hay camino, no dibujamos nada.
        if (path == null || path.Count == 0)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 position =
                path[i].worldPosition + Vector3.up * 0.15f;

            // Dibujamos cada nodo del camino.
            Gizmos.DrawSphere(position, 0.08f);

            // Dibujamos la conexión con el siguiente nodo.
            if (i < path.Count - 1)
            {
                Vector3 nextPosition =
                    path[i + 1].worldPosition
                    + Vector3.up * 0.15f;

                Gizmos.DrawLine(position, nextPosition);
            }
        }
    }
}

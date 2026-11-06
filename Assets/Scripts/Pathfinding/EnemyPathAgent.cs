
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MovementSystemController))]
public class EnemyPathAgent : MonoBehaviour
{
    [Header("Referencias")]
    public GridManager gridManager;
    public AStarPathfinding pathfinding;


    [Header("Orientación")]
    [SerializeField] private Transform pathLookTarget;
    public Transform PathLookTarget => pathLookTarget;



    [Header("Configuración")]
    public float waypointReachedDistance = 0.4f;
    public float pathRefreshInterval = 0.5f;

    private Vector3 destination;
    private Transform followTarget;
    private bool hasDestination = false;

    private List<GridNode> currentPath = new List<GridNode>();
    private int currentWaypointIndex = 0;
    private float nextRefreshTime = 0f;
    private GridNode lastTargetNode;

    private MovementSystemController movementController;

    public bool HasPath { get; private set; }
    public bool PathCalculationFailed { get; private set; }

    private void Awake()
    {
        movementController = GetComponent<MovementSystemController>();
    }

    // Indicar una posición a la que queremos llegar.
    public void SetDestination(Vector3 newDestination)
    {
        if (!hasDestination)
            nextRefreshTime = 0f;

        destination = newDestination;
        followTarget = null;
        hasDestination = true;
    }

    // Seguir a un objeto que puede moverse.
    public void SetDestination(Transform target)
    {
        if (target == null)
        {
            ClearDestination();
            return;
        }

        if (!hasDestination || followTarget != target)
            nextRefreshTime = 0f;

        followTarget = target;
        destination = target.position;
        hasDestination = true;
    }

    public void ClearDestination()
    {
        hasDestination = false;
        followTarget = null;
        currentPath.Clear();
        currentWaypointIndex = 0;
        lastTargetNode = null;
        HasPath = false;
        PathCalculationFailed = false;
    }

    public bool HasReachedDestination()
    {
        if (!hasDestination || currentPath.Count == 0)
            return false;

        Vector3 difference = destination - transform.position;
        difference.y = 0f;

        return difference.magnitude <= waypointReachedDistance;
    }

    private void Update()
    {
        if (!hasDestination)
            return;

        if (gridManager == null || pathfinding == null ||
            gridManager.Grid == null)
            return;

        // Si seguimos a un objeto, actualizar su posición.
        if (followTarget != null)
            destination = followTarget.position;

        GridNode targetNode =
            gridManager.GetNodeFromWorldPosition(destination);

        // Recalcular la ruta periódicamente o al cambiar
        // la casilla de destino.
        if (Time.time >= nextRefreshTime ||
            targetNode != lastTargetNode)
        {
            CalculatePath();
            lastTargetNode = targetNode;

            nextRefreshTime =
                Time.time + Mathf.Max(0.1f, pathRefreshInterval);
        }

        if (currentPath.Count == 0)
        {
            movementController.SetNewTarget(transform.position);
            return;
        }

        // Avanzar al siguiente nodo cuando lleguemos.
        while (currentWaypointIndex < currentPath.Count)
        {
            Vector3 waypoint =
                currentPath[currentWaypointIndex].worldPosition;

            Vector3 difference = waypoint - transform.position;
            difference.y = 0f;

            if (difference.magnitude > waypointReachedDistance)
                break;

            currentWaypointIndex++;
        }

        // Obtener el punto al que debe moverse el enemigo.
        Vector3 nextTarget;

        if (currentWaypointIndex < currentPath.Count)
        {
            nextTarget =
                currentPath[currentWaypointIndex].worldPosition;
        }
        else
        {
            nextTarget = destination;
        }

        nextTarget.y = transform.position.y;

        // Actualizamos el punto hacia el que debe mirar el jabalí.
        if (pathLookTarget != null)
        {
            pathLookTarget.position = nextTarget;
        }

        // Enviar el siguiente punto al controlador de movimiento.
        movementController.SetNewTarget(nextTarget);
    }

    private void CalculatePath()
    {
        currentPath.Clear();
        currentWaypointIndex = 0;
        HasPath = false;
        PathCalculationFailed = false;

        GridNode startNode =
            gridManager.GetNodeFromWorldPosition(transform.position);

        GridNode targetNode =
            gridManager.GetNodeFromWorldPosition(destination);

        if (startNode == null || targetNode == null)
        {
            PathCalculationFailed = true;
            return;
        }

        if (!startNode.isWalkable || !targetNode.isWalkable)
        {
            PathCalculationFailed = true;
            return;
        }

        List<GridNode> newPath =
            pathfinding.FindPath(startNode, targetNode);

        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            currentWaypointIndex = 1;
            HasPath = true;
        }
        else
        {
            PathCalculationFailed = true;
        }
    }

    // Dibujar la ruta en la ventana Scene.
    private void OnDrawGizmosSelected()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Gizmos.color = Color.cyan;

        for (int i = currentWaypointIndex; i < currentPath.Count; i++)
        {
            Vector3 position =
                currentPath[i].worldPosition + Vector3.up * 0.2f;

            Gizmos.DrawSphere(position, 0.1f);

            if (i > 0)
            {
                Vector3 previous =
                    currentPath[i - 1].worldPosition + Vector3.up * 0.2f;

                Gizmos.DrawLine(previous, position);
            }
        }
    }
}

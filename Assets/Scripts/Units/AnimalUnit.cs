
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AnimalType
{
    Jabali,
    Tigre,
    Gorila,
    Cocodrilo,
    Camello
}

public class AnimalUnit : MonoBehaviour
{
    [Header("Unidad")]
    public AnimalType animalType;
    public GridManager gridManager;

    public AStarPathfinding pathfinding;

    [Header("Animaciones")]
    public Animator animator;
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string runState = "Run";

    [Header("Movimiento visual")]
    public float groundOffset = 0.05f;
    public float liftHeight = 0.6f;
    public float walkSpeed = 1.5f;

    public GridNode CurrentNode { get; private set; }

    public bool IsMoving => walkRoutine != null;

    private Coroutine walkRoutine;
    private string currentAnimation = "";

    public int MovementPoints
    {
        get
        {
            switch (animalType)
            {
                case AnimalType.Jabali: return 1;
                case AnimalType.Tigre: return 1;
                case AnimalType.Gorila: return 2;
                case AnimalType.Cocodrilo: return 3;
                case AnimalType.Camello: return 4;
                default: return 1;
            }
        }
    }

    private Vector3 GetGroundPosition(GridNode node)
    {
        return node.worldPosition +
               Vector3.up * groundOffset;
    }

    private void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
        
        if (pathfinding == null)
            pathfinding = FindFirstObjectByType<AStarPathfinding>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        if (gridManager == null || gridManager.Grid == null)
        {
            Debug.LogError("Falta GridManager en " + name);
            enabled = false;
            return;
        }

        GridNode node =
            gridManager.GetNodeFromWorldPosition(transform.position);

        if (node == null ||
            !gridManager.OccupyNode(
                node.gridX, node.gridZ, gameObject))
        {
            Debug.LogError("Casilla inicial invalida: " + name);
            enabled = false;
            return;
        }

        CurrentNode = node;

        // Colocar al animal SOBRE la casilla.
        transform.position = GetGroundPosition(node);

        PlayAnimation(idleState);
    }

    public void Grab(Vector3 mousePosition)
    {
        if (CurrentNode == null) return;

        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
            walkRoutine = null;
        }

        float height =
            GetGroundPosition(CurrentNode).y + liftHeight;

        transform.position = new Vector3(
            mousePosition.x,
            height,
            mousePosition.z
        );

        PlayAnimation(runState);
    }

    
public void Drop(GridNode destination)
{
    if (destination == null || !CanReach(destination))
    {
        ReturnToOrigin();
        return;
    }

    // Si soltamos el animal en su propia casilla.
    if (destination == CurrentNode)
    {
        ReturnToOrigin();
        return;
    }

    // Guardamos el origen antes de actualizar CurrentNode.
    GridNode origin = CurrentNode;

    // Calculamos la ruta que seguirá visualmente.
    List<GridNode> path = pathfinding.FindPath(
        origin,
        destination,
        this
    );

    if (path == null || path.Count == 0)
    {
        ReturnToOrigin();
        return;
    }

    // Reservamos la casilla de destino.
    bool occupied = gridManager.OccupyNode(
        destination.gridX,
        destination.gridZ,
        gameObject
    );

    if (!occupied)
    {
        ReturnToOrigin();
        return;
    }

    // Liberamos la casilla anterior.
    gridManager.FreeNode(
        origin.gridX,
        origin.gridZ,
        gameObject
    );

    CurrentNode = destination;

    // Volvemos visualmente al origen para recorrer la ruta.
    transform.position = GetGroundPosition(origin);

    walkRoutine = StartCoroutine(WalkAlongPath(path));
}


    
private IEnumerator WalkAlongPath(List<GridNode> path)
{
    PlayAnimation(walkState);

    // Empezamos en 1 porque el nodo 0 es el origen.
    for (int i = 1; i < path.Count; i++)
    {
        Vector3 targetPosition =
            GetGroundPosition(path[i]);

        while (Vector3.Distance(
            transform.position,
            targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                walkSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
    }

    walkRoutine = null;

    PlayAnimation(idleState);
}


    private void ReturnToOrigin()
    {
        transform.position = GetGroundPosition(CurrentNode);
        PlayAnimation(idleState);
    }

    private void PlayAnimation(string state)
    {
        if (animator == null ||
            string.IsNullOrEmpty(state))
            return;

        if (currentAnimation == state)
            return;

        animator.Play(state, 0, 0f);

        currentAnimation = state;
    }

    public int GetTerrainCost(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Plain:
                return 1;

            case TerrainType.Desert:
                return animalType == AnimalType.Camello ? 1 :
                       animalType == AnimalType.Gorila ? 3 : 2;

            case TerrainType.Snow:
                return animalType == AnimalType.Tigre ? 1 : 2;

            case TerrainType.Water:
                return animalType == AnimalType.Cocodrilo
                    ? 1 : int.MaxValue;

            default:
                return int.MaxValue;
        }
    }

    
public bool CanReach(GridNode destination)
{
    if (CurrentNode == null || destination == null ||
        pathfinding == null)
        return false;

    // Permanecer en la misma casilla siempre es válido.
    if (destination == CurrentNode)
        return true;

    // No podemos movernos a una casilla bloqueada u ocupada.
    if (!destination.isWalkable || destination.isOccupied)
        return false;

    // Calculamos la ruta utilizando A*.
    List<GridNode> path = pathfinding.FindPath(
        CurrentNode,
        destination,
        this
    );

    if (path == null || path.Count == 0)
        return false;

    int totalCost = 0;

    // La primera casilla es el origen y no tiene coste.
    for (int i = 1; i < path.Count; i++)
    {
        int cost = GetTerrainCost(path[i].terrainType);

        if (cost == int.MaxValue)
            return false;

        totalCost += cost;

        // No podemos superar los puntos de movimiento.
        if (totalCost > MovementPoints)
            return false;
    }

    return true;
}



    private void OnDestroy()
    {
        if (gridManager != null && CurrentNode != null)
        {
            gridManager.FreeNode(
                CurrentNode.gridX,
                CurrentNode.gridZ,
                gameObject
            );
        }
    }
}

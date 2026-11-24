
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
    // Keep the defeated object alive long enough for the death animation.
    public string deathState = "Die";
    [Min(0.1f)] public float deathAnimationDuration = 1f;

    [Header("Movimiento visual")]
    public float groundOffset = 0.05f;
    public float liftHeight = 0.6f;
    public float walkSpeed = 1.5f;

    [Header("Estadisticas de movimiento")]
    // These values are configured per prefab rather than inferred from the
    // animal type, making balancing possible without code changes.
    [Min(1)] public int movementPoints = 1;
    [Min(1)] public int plainMovementCost = 1;
    [Min(1)] public int desertMovementCost = 2;
    [Min(1)] public int snowMovementCost = 2;
    [Min(1)] public int waterMovementCost = 1;

    [Header("Estadisticas de combate")]
    [Min(1)] public int maxHealth = 10;
    [Min(0)] public int attack = 3;
    [Min(0)] public int defense = 0;
    [Min(1)] public int attackRange = 1;

    public GridNode CurrentNode { get; private set; }

    // Visual movement effects also block new actions until they finish.
    public bool IsMoving => walkRoutine != null || attackBumpRoutine != null;
    public bool IsDead => deathRoutine != null;
    public bool HasMovedThisTurn { get; private set; }
    public bool HasAttackedThisTurn { get; private set; }
    public int CurrentHealth { get; private set; }

    private Coroutine walkRoutine;
    // Each effect has its own routine so it can be stopped independently.
    private Coroutine attackBumpRoutine;
    private Coroutine deathRoutine;
    private string currentAnimation = "";

    private Vector3 GetGroundPosition(GridNode node)
    {
        return node.worldPosition +
               Vector3.up * groundOffset;
    }

    private void Start()
    {
        // Runtime health starts from the maximum configured on the prefab.
        CurrentHealth = maxHealth;

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

        if (node == null)
        {
            Debug.LogError(
                "[INICIO UNIDAD] " + name +
                " - No se ha encontrado ninguna casilla. " +
                "Posicion: " + transform.position);
            enabled = false;
            return;
        }

        if (!node.isWalkable || node.isOccupied)
        {
            Debug.LogError(
                "[INICIO UNIDAD] " + name +
                " - Casilla: (" + node.gridX + ", " + node.gridZ + ")" +
                " - Posicion: " + transform.position +
                " - Transitable: " + node.isWalkable +
                " - Ocupada: " + node.isOccupied +
                " - Ocupante: " +
                (node.occupant != null ? node.occupant.name : "Ninguno"));
            enabled = false;
            return;
        }

        if (!gridManager.OccupyNode(node.gridX, node.gridZ, gameObject))
        {
            Debug.LogError(
                "[INICIO UNIDAD] " + name +
                " - No se pudo registrar la ocupacion.");
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
    if (destination == null || destination == CurrentNode)
    {
        Debug.Log("[ANIMALUNIT][DROP] Movimiento cancelado para " + name + ".");
        CancelDrag();
        return;
    }

    List<GridNode> path = FindMovementPath(destination);
    if (!StartMovement(path))
    {
        Debug.Log("[ANIMALUNIT][DROP] Movimiento rechazado para " + name + ".");
        CancelDrag();
        return;
    }
}

/// <summary>
/// Requests an A* route and moves this unit as far along it as its movement
/// allowance permits. The caller supplies the objective for this turn.
/// </summary>
public bool MoveTowardsObjective(GridNode objective)
{
    if (!CanMove() || objective == null ||
        objective == CurrentNode || pathfinding == null)
    {
        return false;
    }

    TurnManager turnManager = FindFirstObjectByType<TurnManager>();
    if (turnManager != null && !turnManager.CanUnitAct(this))
        return false;

    List<GridNode> path = pathfinding.FindPath(
        CurrentNode, objective, this
    );
    if (path == null || path.Count < 2)
        return false;

    int totalCost = 0;
    int lastReachableIndex = 0;
    for (int i = 1; i < path.Count; i++)
    {
        int stepCost = GetTerrainCost(path[i].terrainType);
        if (stepCost == int.MaxValue ||
            stepCost > movementPoints - totalCost)
        {
            break;
        }

        totalCost += stepCost;
        lastReachableIndex = i;
    }

    if (lastReachableIndex == 0)
        return false;

    List<GridNode> movementPath =
        path.GetRange(0, lastReachableIndex + 1);
    return StartMovement(movementPath);
}

private bool StartMovement(List<GridNode> path)
{
    if (!CanMove() || path == null || path.Count < 2 ||
        path[0] != CurrentNode || gridManager == null)
    {
        return false;
    }

    GridNode origin = CurrentNode;
    GridNode destination = path[path.Count - 1];
    if (destination == null || destination.isOccupied ||
        !destination.isWalkable || !PathIsWithinMovementPoints(path))
    {
        return false;
    }

    if (!gridManager.OccupyNode(
        destination.gridX, destination.gridZ, gameObject))
    {
        return false;
    }

    if (!gridManager.FreeNode(
        origin.gridX, origin.gridZ, gameObject))
    {
        gridManager.FreeNode(
            destination.gridX, destination.gridZ, gameObject);
        Debug.LogError(
            "[ANIMALUNIT][MOVE] No se pudo liberar la casilla de origen de " +
            name + ".");
        return false;
    }

    CurrentNode = destination;
    HasMovedThisTurn = true;
    transform.position = GetGroundPosition(origin);
    walkRoutine = StartCoroutine(WalkAlongPath(path));
    return true;
}

    // Restore the visual position without changing grid registration.
    public void CancelDrag()
    {
        transform.position = GetGroundPosition(CurrentNode);
        PlayAnimation(idleState);
    }

    public void PlayAttackBump(Vector3 targetPosition)
    {
        // Combat is already resolved when this feedback animation begins.
        if (attackBumpRoutine != null)
            StopCoroutine(attackBumpRoutine);

        transform.position = GetGroundPosition(CurrentNode);
        attackBumpRoutine = StartCoroutine(AttackBumpRoutine(targetPosition));
    }

    public bool CanMove()
    {
        // A unit may move once per turn and cannot overlap another animation.
        return !IsDead && !HasMovedThisTurn && !HasAttackedThisTurn && !IsMoving;
    }

    public bool CanAttack()
    {
        return !IsDead && !HasAttackedThisTurn && !IsMoving;
    }

    public void ResetTurnActions()
    {
        // TurnManager calls this when the owning player becomes active.
        HasMovedThisTurn = false;
        HasAttackedThisTurn = false;
    }

    public void MarkAttacked()
    {
        HasAttackedThisTurn = true;
    }

    public void ReceiveDamage(int damage)
    {
        // Clamp damage here so every caller follows the same health rules.
        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage));
    }

    public bool IsDefeated()
    {
        return CurrentHealth <= 0;
    }

    public void Die()
    {
        if (!IsDefeated() || IsDead)
            return;

        // Remove the unit from gameplay immediately, then play its death
        // animation before destroying the GameObject.
        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
            walkRoutine = null;
        }

        if (attackBumpRoutine != null)
        {
            StopCoroutine(attackBumpRoutine);
            attackBumpRoutine = null;
        }

        if (gridManager != null && CurrentNode != null)
        {
            gridManager.FreeNode(
                CurrentNode.gridX,
                CurrentNode.gridZ,
                gameObject
            );
        }

        foreach (Collider unitCollider in GetComponentsInChildren<Collider>())
            unitCollider.enabled = false;

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        PlayAnimation(deathState);
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
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

    private IEnumerator AttackBumpRoutine(Vector3 targetPosition)
    {
        // Move a fraction of a cell toward the target and back without
        // changing CurrentNode or grid occupancy.
        Vector3 origin = GetGroundPosition(CurrentNode);
        Vector3 direction = targetPosition - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            direction.Normalize();

        Vector3 bumpPosition = origin + direction * (gridManager.cellSize * 0.25f);
        float duration = 0.12f;
        float elapsed = 0f;

        PlayAnimation(runState);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(origin, bumpPosition, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(bumpPosition, origin, elapsed / duration);
            yield return null;
        }

        transform.position = origin;
        attackBumpRoutine = null;
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
        // Terrain costs are per-prefab values so balance changes stay in the
        // Inspector rather than requiring changes to this shared class.
        switch (terrain)
        {
            case TerrainType.Plain:
                return plainMovementCost;

            case TerrainType.Desert:
                return desertMovementCost;

            case TerrainType.Snow:
                return snowMovementCost;

            case TerrainType.Water:
                return waterMovementCost;

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

    return FindMovementPath(destination).Count > 0;
}

private List<GridNode> FindMovementPath(GridNode destination)
{
    if (CurrentNode == null || destination == null ||
        pathfinding == null || !destination.isWalkable ||
        destination.isOccupied || movementPoints < 0)
    {
        return new List<GridNode>();
    }

    List<GridNode> path = pathfinding.FindPath(
        CurrentNode, destination, this
    );

    return PathIsWithinMovementPoints(path)
        ? path
        : new List<GridNode>();
}

private bool PathIsWithinMovementPoints(List<GridNode> path)
{
    if (path == null || path.Count == 0)
        return false;

    int totalCost = 0;
    for (int i = 1; i < path.Count; i++)
    {
        int cost = GetTerrainCost(path[i].terrainType);
        if (cost == int.MaxValue || cost > movementPoints - totalCost)
            return false;

        totalCost += cost;
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

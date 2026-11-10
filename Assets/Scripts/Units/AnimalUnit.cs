
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
    public string deathState = "Die";
    [Min(0.1f)] public float deathAnimationDuration = 1f;

    [Header("Movimiento visual")]
    public float groundOffset = 0.05f;
    public float liftHeight = 0.6f;
    public float walkSpeed = 1.5f;

    [Header("Estadisticas de movimiento")]
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

    public bool IsMoving => walkRoutine != null || attackBumpRoutine != null;
    public bool IsDead => deathRoutine != null;
    public bool HasMovedThisTurn { get; private set; }
    public bool HasAttackedThisTurn { get; private set; }
    public int CurrentHealth { get; private set; }

    private Coroutine walkRoutine;
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
        Debug.Log("[ANIMALUNIT][DROP] Movimiento rechazado para " + name + ".");
        CancelDrag();
        return;
    }

    // Si soltamos el animal en su propia casilla.
    if (destination == CurrentNode)
    {
        Debug.Log("[ANIMALUNIT][DROP] Movimiento cancelado en la casilla de origen: " + name + ".");
        CancelDrag();
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
        Debug.Log("[ANIMALUNIT][DROP] No se encontro ruta para " + name + ".");
        CancelDrag();
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
        Debug.Log("[ANIMALUNIT][DROP] Casilla de destino ocupada para " + name + ".");
        CancelDrag();
        return;
    }

    // Liberamos la casilla anterior.
    gridManager.FreeNode(
        origin.gridX,
        origin.gridZ,
        gameObject
    );

    CurrentNode = destination;
    HasMovedThisTurn = true;
    Debug.Log("[ANIMALUNIT][DROP] " + name + " se mueve a (" +
        destination.gridX + "," + destination.gridZ + ").");

    // Volvemos visualmente al origen para recorrer la ruta.
    transform.position = GetGroundPosition(origin);

    walkRoutine = StartCoroutine(WalkAlongPath(path));
}

    public void CancelDrag()
    {
        transform.position = GetGroundPosition(CurrentNode);
        PlayAnimation(idleState);
    }

    public void PlayAttackBump(Vector3 targetPosition)
    {
        if (attackBumpRoutine != null)
            StopCoroutine(attackBumpRoutine);

        transform.position = GetGroundPosition(CurrentNode);
        attackBumpRoutine = StartCoroutine(AttackBumpRoutine(targetPosition));
    }

    public bool CanMove()
    {
        return !IsDead && !HasMovedThisTurn && !IsMoving;
    }

    public bool CanAttack()
    {
        return !IsDead && !HasAttackedThisTurn && !IsMoving;
    }

    public void ResetTurnActions()
    {
        HasMovedThisTurn = false;
        HasAttackedThisTurn = false;
    }

    public void MarkAttacked()
    {
        HasAttackedThisTurn = true;
    }

    public void ReceiveDamage(int damage)
    {
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
        if (totalCost > movementPoints)
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

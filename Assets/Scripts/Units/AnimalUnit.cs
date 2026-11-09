
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

        if (destination != CurrentNode)
        {
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

            gridManager.FreeNode(
                CurrentNode.gridX,
                CurrentNode.gridZ,
                gameObject
            );

            CurrentNode = destination;
        }

        // Soltar el animal sobre la superficie.
        Vector3 groundPosition =
            GetGroundPosition(CurrentNode);

        transform.position = new Vector3(
            transform.position.x,
            groundPosition.y,
            transform.position.z
        );

        walkRoutine = StartCoroutine(WalkToCenter());
    }

    private IEnumerator WalkToCenter()
    {
        PlayAnimation(walkState);

        Vector3 center = GetGroundPosition(CurrentNode);

        while (Vector3.Distance(
            transform.position, center) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                center,
                walkSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = center;

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

    private int GetTerrainCost(TerrainType terrain)
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
        if (CurrentNode == null || destination == null)
            return false;

        if (destination == CurrentNode)
            return true;

        if (!destination.isWalkable || destination.isOccupied)
            return false;

        Dictionary<GridNode, int> costs =
            new Dictionary<GridNode, int>();

        List<GridNode> pending = new List<GridNode>();

        costs[CurrentNode] = 0;
        pending.Add(CurrentNode);

        while (pending.Count > 0)
        {
            GridNode current = pending[0];

            foreach (GridNode node in pending)
            {
                if (costs[node] < costs[current])
                    current = node;
            }

            pending.Remove(current);

            if (current == destination)
                return true;

            foreach (GridNode neighbor in current.neighbors)
            {
                if (!neighbor.isWalkable ||
                    neighbor.isOccupied)
                    continue;

                if (!CanPassDiagonal(current, neighbor))
                    continue;

                int cost =
                    GetTerrainCost(neighbor.terrainType);

                if (cost == int.MaxValue)
                    continue;

                int newCost = costs[current] + cost;

                if (newCost > MovementPoints)
                    continue;

                if (!costs.ContainsKey(neighbor) ||
                    newCost < costs[neighbor])
                {
                    costs[neighbor] = newCost;

                    if (!pending.Contains(neighbor))
                        pending.Add(neighbor);
                }
            }
        }

        return false;
    }

    private bool CanPassDiagonal(GridNode from, GridNode to)
    {
        int dx = to.gridX - from.gridX;
        int dz = to.gridZ - from.gridZ;

        if (dx == 0 || dz == 0)
            return true;

        GridNode sideA = gridManager.GetNode(
            from.gridX + dx, from.gridZ);

        GridNode sideB = gridManager.GetNode(
            from.gridX, from.gridZ + dz);

        if (sideA == null || sideB == null)
            return false;

        return sideA.isWalkable && !sideA.isOccupied &&
               sideB.isWalkable && !sideB.isOccupied;
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

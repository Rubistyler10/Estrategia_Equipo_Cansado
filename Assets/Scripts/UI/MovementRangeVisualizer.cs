using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Previsualiza el movimiento sin cambiar las reglas de AnimalUnit.
/// Azul = jugador 1; rojo = jugador 2.
/// Durante el arrastre, el alcance sigue anclado a CurrentNode (origen real)
/// y una marca adicional sigue al cursor para indicar el destino propuesto.
/// </summary>
public class MovementRangeVisualizer : MonoBehaviour
{
    [Header("Referencias")]
    public Camera gameCamera;
    public GridManager gridManager;
    public AnimalDragController dragController;

    [Header("Sprites recortados de HOJA_COMPLETA")]
    public Sprite casillaMovimientoAzul;
    public Sprite casillaMovimientoRoja;
    public Sprite casillaPrevisualizacion; // Casilla_Hover_Amarillo

    [Header("Posicion de las casillas")]
    [Range(0.6f, 1f)] public float cellFill = 0.94f;
    public float heightAboveNode = 1.03f;
    [Range(0.1f, 1f)] public float movementOpacity = 0.65f;

    [Header("Color del destino durante el arrastre")]
    public Color validDestinationColor = new Color(0.30f, 1f, 0.40f, 0.95f);
    public Color attackDestinationColor = new Color(1f, 0.72f, 0.25f, 0.95f);
    public Color invalidDestinationColor = new Color(1f, 0.28f, 0.30f, 0.90f);

    private readonly List<SpriteRenderer> pooledCells = new List<SpriteRenderer>();
    private readonly HashSet<GridNode> reachableNodes = new HashSet<GridNode>();
    private SpriteRenderer previewCell;
    private int visibleCells;

    private AnimalUnit shownUnit;
    private GridNode shownOrigin;
    private int shownPlayerId = -1;
    private int shownMovementPoints = -1;
    private bool shownMovementAllowed;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (dragController == null)
            dragController = FindFirstObjectByType<AnimalDragController>();

        previewCell = CreateVisual("DestinoDuranteArrastre", 25);
        previewCell.enabled = false;
    }

    private void LateUpdate()
    {
        if (gameCamera == null || gridManager == null ||
            gridManager.Grid == null || gridManager.ground == null)
        {
            HideAll();
            return;
        }

        AnimalUnit draggedUnit = dragController != null
            ? dragController.SelectedAnimal : null;
        bool isDragging = draggedUnit != null;

        // El arrastre tiene preferencia: el modelo puede estar ya bajo el raton
        // en otra casilla, pero su origen valido continua siendo CurrentNode.
        AnimalUnit unit = isDragging ? draggedUnit : GetHoveredUnit();
        if (unit == null || !unit.isActiveAndEnabled ||
            unit.CurrentNode == null || unit.IsDead)
        {
            HideAll();
            return;
        }

        PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
        if (owner == null)
        {
            HideAll();
            return;
        }

        bool canShowMovement = !isDragging || unit.CanMove();
        if (shownUnit != unit || shownOrigin != unit.CurrentNode ||
            shownPlayerId != owner.playerId ||
            shownMovementPoints != unit.movementPoints ||
            shownMovementAllowed != canShowMovement)
        {
            RebuildRange(unit, owner.playerId, canShowMovement);
        }

        if (isDragging)
            UpdateDragPreview(unit);
        else
            previewCell.enabled = false;
    }

    private void RebuildRange(AnimalUnit unit, int playerId, bool canShowMovement)
    {
        HideRange();
        reachableNodes.Clear();

        shownUnit = unit;
        shownOrigin = unit.CurrentNode;
        shownPlayerId = playerId;
        shownMovementPoints = unit.movementPoints;
        shownMovementAllowed = canShowMovement;

        if (!canShowMovement)
            return;

        Sprite sprite = playerId == 1
            ? casillaMovimientoAzul : casillaMovimientoRoja;

        if (sprite == null)
        {
            Debug.LogWarning("MovementRangeVisualizer: falta asignar el sprite de movimiento del jugador " + playerId);
            return;
        }

        // Como el coste minimo de cada paso es 1, ningun destino puede
        // estar a mas de movementPoints casillas de distancia (8 direcciones).
        // Evitamos recorrer toda la cuadrícula y recalcular A* cada frame.
        GridNode origin = unit.CurrentNode;
        int radius = Mathf.Max(0, unit.movementPoints);
        int minX = Mathf.Max(0, origin.gridX - radius);
        int maxX = Mathf.Min(gridManager.width - 1, origin.gridX + radius);
        int minZ = Mathf.Max(0, origin.gridZ - radius);
        int maxZ = Mathf.Min(gridManager.depth - 1, origin.gridZ + radius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                GridNode node = gridManager.GetNode(x, z);
                if (node == null || node == origin ||
                    !node.isWalkable || node.isOccupied)
                    continue;

                // CanReach es la misma validacion usada en AnimalUnit.Drop:
                // ruta A*, costes de terreno, obstaculos y puntos de movimiento.
                if (!unit.CanReach(node))
                    continue;

                reachableNodes.Add(node);
                SpriteRenderer cell = GetPooledCell(visibleCells++);
                PlaceCell(cell, node, sprite, movementOpacity, heightAboveNode);
            }
        }
    }

    private void UpdateDragPreview(AnimalUnit unit)
    {
        if (casillaPrevisualizacion == null || IsOverUI() ||
            !TryGetBoardNode(out GridNode target) || target == unit.CurrentNode)
        {
            previewCell.enabled = false;
            return;
        }

        // Verde: casilla realmente alcanzable desde el origen.
        // Dorado: objetivo enemigo al que la unidad puede atacar.
        // Rojo: destino no valido (no gastar movimiento ni actuar).
        Color color = reachableNodes.Contains(target)
            ? validDestinationColor
            : CanAttackTarget(unit, target)
                ? attackDestinationColor
                : invalidDestinationColor;

        PlaceCell(previewCell, target, casillaPrevisualizacion, color.a,
            heightAboveNode + 0.04f);
        previewCell.color = color;
    }

    private bool CanAttackTarget(AnimalUnit attacker, GridNode target)
    {
        if (!attacker.CanAttack() || target == null ||
            !target.isOccupied || target.occupant == null)
            return false;

        PlayerOwnership attackerOwner = attacker.GetComponent<PlayerOwnership>();
        if (attackerOwner == null)
            return false;

        AnimalUnit otherUnit = target.occupant.GetComponent<AnimalUnit>();
        PlayerBase otherBase = target.occupant.GetComponent<PlayerBase>();

        int otherPlayer;
        if (otherUnit != null)
        {
            PlayerOwnership targetOwner = otherUnit.GetComponent<PlayerOwnership>();
            if (targetOwner == null || otherUnit.IsDead)
                return false;
            otherPlayer = targetOwner.playerId;
        }
        else if (otherBase != null && !otherBase.IsDestroyed)
        {
            otherPlayer = otherBase.playerId;
        }
        else
        {
            return false;
        }

        int dx = Mathf.Abs(attacker.CurrentNode.gridX - target.gridX);
        int dz = Mathf.Abs(attacker.CurrentNode.gridZ - target.gridZ);
        return otherPlayer != attackerOwner.playerId &&
               Mathf.Max(dx, dz) <= attacker.attackRange;
    }

    private AnimalUnit GetHoveredUnit()
    {
        if (IsOverUI())
            return null;

        Vector2 pointer = MousePosition();
        if (!IsOnScreen(pointer))
            return null;

        Ray ray = gameCamera.ScreenPointToRay(pointer);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f,
                ~0, QueryTriggerInteraction.Ignore))
            return null;

        return hit.collider.GetComponentInParent<AnimalUnit>();
    }

    private bool TryGetBoardNode(out GridNode node)
    {
        node = null;
        Vector2 pointer = MousePosition();
        if (!IsOnScreen(pointer))
            return false;

        Ray ray = gameCamera.ScreenPointToRay(pointer);
        Plane boardPlane = new Plane(Vector3.up, gridManager.ground.position);
        if (!boardPlane.Raycast(ray, out float distance))
            return false;

        Vector3 point = ray.GetPoint(distance);
        Vector3 center = gridManager.ground.position;
        float halfX = gridManager.width * gridManager.cellSize / 2f;
        float halfZ = gridManager.depth * gridManager.cellSize / 2f;

        if (point.x < center.x - halfX || point.x >= center.x + halfX ||
            point.z < center.z - halfZ || point.z >= center.z + halfZ)
            return false;

        node = gridManager.GetNodeFromWorldPosition(point);
        return node != null;
    }

    private SpriteRenderer GetPooledCell(int index)
    {
        while (pooledCells.Count <= index)
            pooledCells.Add(CreateVisual("CasillaAlcanzable_" + pooledCells.Count, 10));
        return pooledCells[index];
    }

    private SpriteRenderer CreateVisual(string visualName, int sortingOrder)
    {
        GameObject child = new GameObject(visualName);
        child.transform.SetParent(transform, false);
        child.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;
        // Sin Collider: no bloquea raycasts, seleccion ni el sistema de terrenos.
        return renderer;
    }

    private void PlaceCell(SpriteRenderer cell, GridNode node,
        Sprite sprite, float opacity, float yOffset)
    {
        if (sprite == null)
        {
            cell.enabled = false;
            return;
        }

        cell.sprite = sprite;
        cell.color = new Color(1f, 1f, 1f, opacity);
        cell.transform.position = node.worldPosition + Vector3.up * yOffset;
        cell.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        Vector2 bounds = sprite.bounds.size;
        float targetSize = gridManager.cellSize * cellFill;
        if (bounds.x > 0f && bounds.y > 0f)
        {
            cell.transform.localScale = new Vector3(
                targetSize / bounds.x,
                targetSize / bounds.y,
                1f);
        }
        cell.enabled = true;
    }

    private void HideRange()
    {
        for (int i = 0; i < visibleCells; i++)
            pooledCells[i].enabled = false;
        visibleCells = 0;
    }

    private void HideAll()
    {
        HideRange();
        reachableNodes.Clear();
        if (previewCell != null)
            previewCell.enabled = false;
        shownUnit = null;
        shownOrigin = null;
        shownPlayerId = -1;
        shownMovementPoints = -1;
    }

    private void OnDisable()
    {
        HideAll();
    }

    private static bool IsOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsOnScreen(Vector2 pointer)
    {
        return pointer.x >= 0f && pointer.y >= 0f &&
               pointer.x < Screen.width && pointer.y < Screen.height;
    }

    private static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue()
            : new Vector2(-1f, -1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector2(-1f, -1f);
#endif
    }
}

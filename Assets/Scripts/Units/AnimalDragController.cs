
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AnimalDragController : MonoBehaviour
{
    [Header("Referencias")]
    public Camera gameCamera;
    public GridManager gridManager;
    public TurnManager turnManager;
    public CombatSystem combatSystem;

    private AnimalUnit selectedAnimal;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
        if (combatSystem == null)
            combatSystem = FindFirstObjectByType<CombatSystem>();
    }

    private void Update()
    {
        if (gameCamera == null || gridManager == null ||
            gridManager.ground == null)
            return;

        // Seleccionar animal
        if (MouseDown() && selectedAnimal == null)
        {
            Ray ray = gameCamera.ScreenPointToRay(MousePosition());

            if (Physics.Raycast(
                ray, out RaycastHit hit, 1000f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                AnimalUnit animal =
                    hit.collider.GetComponentInParent<AnimalUnit>();

                if (animal != null &&
                    animal.isActiveAndEnabled &&
                    animal.CurrentNode != null &&
                    !animal.IsMoving &&
                    (animal.CanMove() || animal.CanAttack()) &&
                    (turnManager == null || turnManager.CanUnitAct(animal)))
                {
                    selectedAnimal = animal;
                    Debug.Log("[ANIMALDRAGCONTROLLER][SELECT] Unidad seleccionada: " +
                        animal.name + ".");
                }
            }
        }

        if (selectedAnimal == null)
            return;

        // Arrastrar animal en el aire
        if (MouseHeld() &&
            GetMouseWorldPosition(out Vector3 dragPoint))
        {
            selectedAnimal.Grab(dragPoint);
        }

        // Soltar animal en una casilla
        if (MouseUp())
        {
            GridNode destination = null;

            if (GetMouseWorldPosition(out Vector3 dropPoint) &&
                IsInsideBoard(dropPoint))
            {
                destination =
                    gridManager.GetNodeFromWorldPosition(dropPoint);
            }

            bool actionTaken = false;

            if (destination != null && destination.isOccupied &&
                destination.occupant != null && combatSystem != null)
            {
                AnimalUnit defender =
                    destination.occupant.GetComponent<AnimalUnit>();
                if (defender != null)
                    actionTaken = combatSystem.TryAttack(
                        selectedAnimal, defender);
                else
                {
                    PlayerBase baseTarget =
                        destination.occupant.GetComponent<PlayerBase>();
                    actionTaken = combatSystem.TryAttack(
                        selectedAnimal, baseTarget);
                }
            }
            else
            {
                if (selectedAnimal.CanMove())
                {
                    selectedAnimal.Drop(destination);
                    actionTaken = destination != null &&
                        selectedAnimal.CurrentNode == destination;
                }
            }

            if (!actionTaken && !selectedAnimal.IsMoving)
                selectedAnimal.CancelDrag();

            selectedAnimal = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (selectedAnimal == null || gridManager == null ||
            gridManager.Grid == null || selectedAnimal.CurrentNode == null)
            return;

        if (selectedAnimal.CanMove())
        {
            Gizmos.color = new Color(0.1f, 1f, 0.2f, 0.9f);
            foreach (GridNode node in gridManager.Grid)
            {
                if (!node.isOccupied && selectedAnimal.CanReach(node))
                    DrawNodeGizmo(node, 0.18f);
            }
        }
        else if (selectedAnimal.HasMovedThisTurn && selectedAnimal.CanAttack())
        {
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.9f);
            foreach (GridNode node in gridManager.Grid)
            {
                int xDistance = Mathf.Abs(
                    node.gridX - selectedAnimal.CurrentNode.gridX);
                int zDistance = Mathf.Abs(
                    node.gridZ - selectedAnimal.CurrentNode.gridZ);

                if (Mathf.Max(xDistance, zDistance) <= selectedAnimal.attackRange)
                    DrawNodeGizmo(node, 0.2f);
            }
        }
    }

    private void DrawNodeGizmo(GridNode node, float height)
    {
        Gizmos.DrawCube(
            node.worldPosition + Vector3.up * height,
            new Vector3(gridManager.cellSize * 0.75f, 0.03f,
                gridManager.cellSize * 0.75f));
    }

    private bool GetMouseWorldPosition(out Vector3 position)
    {
        Ray ray = gameCamera.ScreenPointToRay(MousePosition());

        Plane plane = new Plane(
            Vector3.up,
            gridManager.ground.position
        );

        if (plane.Raycast(ray, out float distance))
        {
            position = ray.GetPoint(distance);
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private bool IsInsideBoard(Vector3 point)
    {
        float halfWidth =
            gridManager.width * gridManager.cellSize / 2f;

        float halfDepth =
            gridManager.depth * gridManager.cellSize / 2f;

        Vector3 center = gridManager.ground.position;

        return point.x >= center.x - halfWidth &&
               point.x < center.x + halfWidth &&
               point.z >= center.z - halfDepth &&
               point.z < center.z + halfDepth;
    }

    private static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private static bool MouseDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private static bool MouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    private static bool MouseUp()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonUp(0);
#else
        return false;
#endif
    }
}

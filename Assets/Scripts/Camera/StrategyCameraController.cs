
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class StrategyCameraController : MonoBehaviour
{
    [Header("Referencias")]
    public GridManager gridManager;
    public TurnManager turnManager;
    public AnimalDragController dragController;

    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float shiftMultiplier = 2.5f;
    public float positionSmoothness = 6f;
    public float edgeMargin = 1f;

    [Header("Zoom por niveles")]
    public float[] zoomLevels = { 9f, 13f, 18f, 24f, 30f };
    public int startingZoomIndex = 2;
    public float zoomSmoothness = 7f;

    [Header("Rotacion")]
    public float rotationSensitivity = 0.18f;
    public float rotationSmoothness = 9f;
    public float minPitch = 35f;
    public float maxPitch = 75f;

    [Header("Camara cinematografica")]
    public bool focusOnSelection = true;
    public bool followMovingUnit = true;
    public float cinematicZoom = 10f;

    private Camera cam;
    private Vector3 currentFocus;
    private Vector3 targetFocus;
    private Vector3 previousFocus;

    private float currentDistance;
    private float targetDistance;
    private float previousDistance;

    private float currentYaw;
    private float targetYaw;
    private float currentPitch;
    private float targetPitch;

    private int zoomIndex;
    private int previousPlayer = -1;

    private AnimalUnit trackedUnit;
    private bool ignoreSelectionUntilRelease;


    [Header("Presentacion inicial")]
    public float initialOverviewDistance = 55f;

    private bool externalInputLocked;

    public void SetInputLocked(bool locked)
    {
        externalInputLocked = locked;
    }

    public void FocusOnPlayer(int playerId)
    {
        trackedUnit = null;
        ignoreSelectionUntilRelease = true;

        targetFocus = GetPlayerFocus(playerId);

        zoomIndex = Mathf.Clamp(
            startingZoomIndex, 0, zoomLevels.Length - 1);

        targetDistance = zoomLevels[zoomIndex];
        previousPlayer = playerId;
    }
    

    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = false;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (dragController == null)
            dragController =
                FindFirstObjectByType<AnimalDragController>();

        if (gridManager == null || gridManager.ground == null)
        {
            Debug.LogError("Falta GridManager en la camara.");
            enabled = false;
            return;
        }

        if (zoomLevels == null || zoomLevels.Length == 0)
            zoomLevels = new float[] { 9f, 13f, 18f, 24f, 30f };

        zoomIndex = Mathf.Clamp(
            startingZoomIndex, 0, zoomLevels.Length - 1);

        currentYaw = targetYaw = transform.eulerAngles.y;
        currentPitch = targetPitch = Mathf.Clamp(
            transform.eulerAngles.x, minPitch, maxPitch);

        currentDistance = targetDistance = initialOverviewDistance; // currentDistance = targetDistance = zoomLevels[zoomIndex];

        currentFocus = targetFocus =
            gridManager.ground.position;

        if (turnManager != null)
            previousPlayer = turnManager.currentPlayerId;

        ApplyCameraTransform();
    }

    private void LateUpdate()
    {
        if (externalInputLocked)
            return;
        
        CheckTurnChange();

        Vector2 movement = ReadMovement();
        bool manualMovement = movement.sqrMagnitude > 0.001f;

        if (manualMovement)
        {
            // El jugador recupera el control manual.
            trackedUnit = null;
            ignoreSelectionUntilRelease = true;

            float speed = moveSpeed *
                (IsShiftPressed() ? shiftMultiplier : 1f);

            Vector3 direction =
                Quaternion.Euler(0f, targetYaw, 0f) *
                new Vector3(movement.x, 0f, movement.y);

            targetFocus += direction.normalized *
                speed * Time.unscaledDeltaTime;
        }

        if (dragController == null ||
            dragController.SelectedAnimal == null)
        {
            ignoreSelectionUntilRelease = false;
        }

        UpdateUnitFocus();

        float wheel = ReadScroll();

        if (Mathf.Abs(wheel) > 0.01f && trackedUnit == null)
        {
            zoomIndex += wheel > 0f ? -1 : 1;
            zoomIndex = Mathf.Clamp(
                zoomIndex, 0, zoomLevels.Length - 1);

            targetDistance = zoomLevels[zoomIndex];
        }

        if (IsRightMousePressed() && !IsPointerOverUI())
        {
            Vector2 delta = ReadMouseDelta();

            targetYaw += delta.x * rotationSensitivity;
            targetPitch -= delta.y * rotationSensitivity;
            targetPitch = Mathf.Clamp(
                targetPitch, minPitch, maxPitch);
        }

        targetFocus = ClampToBoard(targetFocus);

        float dt = Time.unscaledDeltaTime;

        float positionT = 1f -
            Mathf.Exp(-positionSmoothness * dt);

        float zoomT = 1f -
            Mathf.Exp(-zoomSmoothness * dt);

        float rotationT = 1f -
            Mathf.Exp(-rotationSmoothness * dt);

        currentFocus = Vector3.Lerp(
            currentFocus, targetFocus, positionT);

        currentDistance = Mathf.Lerp(
            currentDistance, targetDistance, zoomT);

        currentYaw = Mathf.LerpAngle(
            currentYaw, targetYaw, rotationT);

        currentPitch = Mathf.Lerp(
            currentPitch, targetPitch, rotationT);

        ApplyCameraTransform();
    }

    private void CheckTurnChange()
    {
        if (turnManager == null ||
            turnManager.currentPlayerId == previousPlayer)
            return;

        previousPlayer = turnManager.currentPlayerId;
        trackedUnit = null;

        targetFocus = GetPlayerFocus(previousPlayer);

        zoomIndex = Mathf.Clamp(
            startingZoomIndex, 0, zoomLevels.Length - 1);

        targetDistance = zoomLevels[zoomIndex];
    }

    private void UpdateUnitFocus()
    {
        AnimalUnit selected = dragController != null
            ? dragController.SelectedAnimal
            : null;

        if (selected != null &&
            trackedUnit == null &&
            !ignoreSelectionUntilRelease &&
            focusOnSelection)
        {
            trackedUnit = selected;
            previousFocus = targetFocus;
            previousDistance = targetDistance;

            if (selected.CurrentNode != null)
                targetFocus = selected.CurrentNode.worldPosition;

            targetDistance = cinematicZoom;
        }

        if (trackedUnit == null)
            return;

        // Durante el arrastre no perseguimos al raton.
        if (selected == trackedUnit)
            return;

        if (followMovingUnit && trackedUnit.IsMoving)
        {
            targetFocus = trackedUnit.transform.position;
            return;
        }

        // Al terminar, recuperamos el encuadre anterior.
        targetFocus = previousFocus;
        targetDistance = previousDistance;

        trackedUnit = null;
    }

    private Vector3 GetPlayerFocus(int playerId)
    {
        AnimalUnit[] units =
            FindObjectsByType<AnimalUnit>(
                FindObjectsSortMode.None);

        Vector3 total = Vector3.zero;
        int count = 0;

        foreach (AnimalUnit unit in units)
        {
            if (unit == null || unit.IsDead ||
                unit.CurrentNode == null)
                continue;

            PlayerOwnership owner =
                unit.GetComponent<PlayerOwnership>();

            if (owner == null || owner.playerId != playerId)
                continue;

            total += unit.CurrentNode.worldPosition;
            count++;
        }

        if (count > 0)
            return total / count;

        // Si no tiene unidades, intentamos centrar su base.
        PlayerBase[] bases =
            FindObjectsByType<PlayerBase>(
                FindObjectsSortMode.None);

        foreach (PlayerBase playerBase in bases)
        {
            if (playerBase.playerId == playerId)
                return playerBase.transform.position;
        }

        return gridManager.ground.position;
    }

    private Vector3 ClampToBoard(Vector3 position)
    {
        Vector3 center = gridManager.ground.position;

        float halfWidth = Mathf.Max(
            0f,
            gridManager.width * gridManager.cellSize *
            0.5f - edgeMargin);

        float halfDepth = Mathf.Max(
            0f,
            gridManager.depth * gridManager.cellSize *
            0.5f - edgeMargin);

        position.x = Mathf.Clamp(
            position.x,
            center.x - halfWidth,
            center.x + halfWidth);

        position.z = Mathf.Clamp(
            position.z,
            center.z - halfDepth,
            center.z + halfDepth);

        position.y = center.y;

        return position;
    }

    private void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(
            currentPitch, currentYaw, 0f);

        transform.rotation = rotation;
        transform.position = currentFocus -
            rotation * Vector3.forward * currentDistance;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();
    }

    private static Vector2 ReadMovement()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x--;
        if (Keyboard.current.dKey.isPressed) x++;
        if (Keyboard.current.sKey.isPressed) y--;
        if (Keyboard.current.wKey.isPressed) y++;

        return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }

    private static bool IsShiftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed ||
             Keyboard.current.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private static float ReadScroll()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.scroll.ReadValue().y
            : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetAxisRaw("Mouse ScrollWheel");
#else
        return 0f;
#endif
    }

    private static bool IsRightMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
            Mouse.current.rightButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(1);
#else
        return false;
#endif
    }

    private static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.delta.ReadValue()
            : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(
            Input.GetAxis("Mouse X") * 12f,
            Input.GetAxis("Mouse Y") * 12f);
#else
        return Vector2.zero;
#endif
    }
}

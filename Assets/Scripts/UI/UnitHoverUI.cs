using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UnitHoverUI : MonoBehaviour
{
    [Header("Referencias")]
    public Camera gameCamera;
    public GridManager gridManager;
    public AnimalDragController dragController;

    [Header("Panel de unidad")]
    public GameObject panelInfoUnidad;
    public TMP_Text textoNombre;
    public TMP_Text textoVida;
    public TMP_Text textoMovimiento;
    public Image imagenRetrato;

    [Header("Retratos (opcional)")]
    public Sprite retratoJabali;
    public Sprite retratoTigre;
    public Sprite retratoGorila;
    public Sprite retratoCocodrilo;
    public Sprite retratoCamello;

    private AnimalUnit unidadMostrada;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (dragController == null)
            dragController = FindFirstObjectByType<AnimalDragController>();

        // El panel es puramente informativo y no debe tapar el cursor.
        if (panelInfoUnidad != null)
        {
            CanvasGroup group = panelInfoUnidad.GetComponent<CanvasGroup>();
            if (group == null)
                group = panelInfoUnidad.AddComponent<CanvasGroup>();

            group.interactable = false;
            group.blocksRaycasts = false;
            panelInfoUnidad.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (panelInfoUnidad == null || gameCamera == null ||
            gridManager == null || gridManager.Grid == null ||
            gridManager.ground == null)
        {
            OcultarPanel();
            return;
        }

        AnimalUnit unidad = null;

        // Durante el arrastre seguimos mostrando los datos del animal agarrado.
        if (dragController != null && dragController.SelectedAnimal != null)
        {
            unidad = dragController.SelectedAnimal;
        }
        else
        {
            // El panel no se muestra mientras se usan botones del HUD.
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                OcultarPanel();
                return;
            }

            Vector2 mouse = GetMousePosition();
            if (mouse.x < 0 || mouse.y < 0 ||
                mouse.x >= Screen.width || mouse.y >= Screen.height)
            {
                OcultarPanel();
                return;
            }

            Ray ray = gameCamera.ScreenPointToRay(mouse);

            // Prioridad: collider del propio animal.
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f,
                ~0, QueryTriggerInteraction.Ignore))
            {
                unidad = hit.collider.GetComponentInParent<AnimalUnit>();
            }

            // Alternativa: consultar quién ocupa la casilla bajo el cursor.
            if (unidad == null)
            {
                Plane boardPlane = new Plane(
                    Vector3.up, gridManager.ground.position);

                if (boardPlane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    if (IsInsideBoard(point))
                    {
                        GridNode node =
                            gridManager.GetNodeFromWorldPosition(point);
                        if (node != null && node.occupant != null)
                            unidad = node.occupant.GetComponent<AnimalUnit>();
                    }
                }
            }
        }

        if (unidad == null || !unidad.isActiveAndEnabled ||
            unidad.CurrentNode == null || unidad.IsDead)
        {
            OcultarPanel();
            return;
        }

        MostrarUnidad(unidad);
    }

    private void MostrarUnidad(AnimalUnit unidad)
    {
        if (unidadMostrada != unidad)
        {
            unidadMostrada = unidad;

            if (textoNombre != null)
                textoNombre.text = GetAnimalName(unidad.animalType);

            if (imagenRetrato != null)
            {
                Sprite sprite = GetPortrait(unidad.animalType);
                imagenRetrato.sprite = sprite;
                imagenRetrato.gameObject.SetActive(sprite != null);
            }
        }

        if (!panelInfoUnidad.activeSelf)
            panelInfoUnidad.SetActive(true);

        // La vida puede cambiar sin que el cursor abandone el animal.
        if (textoVida != null)
            textoVida.text = "PV " + unidad.CurrentHealth +
                             " / " + unidad.maxHealth;

        if (textoMovimiento != null)
            textoMovimiento.text = "Mov " + unidad.movementPoints;
    }

    private void OcultarPanel()
    {
        unidadMostrada = null;
        if (panelInfoUnidad != null && panelInfoUnidad.activeSelf)
            panelInfoUnidad.SetActive(false);
    }

    private string GetAnimalName(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Jabali: return "Jabalí";
            case AnimalType.Tigre: return "Tigre";
            case AnimalType.Gorila: return "Gorila";
            case AnimalType.Cocodrilo: return "Cocodrilo";
            case AnimalType.Camello: return "Camello";
            default: return type.ToString();
        }
    }

    private Sprite GetPortrait(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Jabali: return retratoJabali;
            case AnimalType.Tigre: return retratoTigre;
            case AnimalType.Gorila: return retratoGorila;
            case AnimalType.Cocodrilo: return retratoCocodrilo;
            case AnimalType.Camello: return retratoCamello;
            default: return null;
        }
    }

    private bool IsInsideBoard(Vector3 point)
    {
        Vector3 center = gridManager.ground.position;
        float halfWidth = gridManager.width * gridManager.cellSize * 0.5f;
        float halfDepth = gridManager.depth * gridManager.cellSize * 0.5f;

        return point.x >= center.x - halfWidth &&
               point.x < center.x + halfWidth &&
               point.z >= center.z - halfDepth &&
               point.z < center.z + halfDepth;
    }

    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(-1f, -1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return new Vector2(-1f, -1f);
#endif
    }

    private void OnDisable()
    {
        OcultarPanel();
    }
}

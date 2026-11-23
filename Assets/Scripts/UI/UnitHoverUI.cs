
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
    public TMP_Text textoAtaque;
    public Image imagenRetrato;

    [Header("Fondo del panel segun propietario")]
    public Image imagenFondoUnidad;
    public Sprite fondoJugador1;
    public Sprite fondoJugador2;

    [Header("Retratos")]
    public Sprite retratoJabali;
    public Sprite retratoTigre;
    public Sprite retratoGorila;
    public Sprite retratoCocodrilo;
    public Sprite retratoCamello;

    private AnimalUnit unidadMostrada;
    private AnimalHealthRing anilloResaltado;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (dragController == null)
            dragController = FindFirstObjectByType<AnimalDragController>();

        if (panelInfoUnidad != null)
        {
            CanvasGroup group =
                panelInfoUnidad.GetComponent<CanvasGroup>();

            if (group == null)
                group = panelInfoUnidad.AddComponent<CanvasGroup>();

            group.interactable = false;
            group.blocksRaycasts = false;

            panelInfoUnidad.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (panelInfoUnidad == null ||
            gameCamera == null ||
            gridManager == null ||
            gridManager.Grid == null ||
            gridManager.ground == null)
        {
            OcultarPanel();
            return;
        }

        AnimalUnit unidad = null;

        if (dragController != null &&
            dragController.SelectedAnimal != null)
        {
            // Conservamos las estadisticas durante el arrastre.
            unidad = dragController.SelectedAnimal;
        }
        else
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                OcultarPanel();
                return;
            }

            Vector2 mouse = GetMousePosition();

            if (mouse.x < 0f ||
                mouse.y < 0f ||
                mouse.x >= Screen.width ||
                mouse.y >= Screen.height)
            {
                OcultarPanel();
                return;
            }

            Ray ray = gameCamera.ScreenPointToRay(mouse);

            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                1000f,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                unidad =
                    hit.collider.GetComponentInParent<AnimalUnit>();
            }

            if (unidad == null)
            {
                Plane boardPlane = new Plane(
                    Vector3.up,
                    gridManager.ground.position
                );

                if (boardPlane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);

                    if (IsInsideBoard(point))
                    {
                        GridNode node =
                            gridManager.GetNodeFromWorldPosition(point);

                        if (node != null && node.occupant != null)
                        {
                            unidad =
                                node.occupant.GetComponent<AnimalUnit>();
                        }
                    }
                }
            }
        }

        if (unidad == null ||
            !unidad.isActiveAndEnabled ||
            unidad.CurrentNode == null ||
            unidad.IsDead)
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
            DesactivarHoverAnterior();

            unidadMostrada = unidad;

            // Cambiar el aro al hacer hover.
            anilloResaltado =
                unidad.GetComponent<AnimalHealthRing>();

            if (anilloResaltado != null)
                anilloResaltado.SetHovered(true);

            // Actualizar nombre.
            if (textoNombre != null)
            {
                textoNombre.text =
                    GetAnimalName(unidad.animalType);
            }

            // Actualizar retrato.
            if (imagenRetrato != null)
            {
                Sprite sprite =
                    GetPortrait(unidad.animalType);

                imagenRetrato.sprite = sprite;

                imagenRetrato.gameObject.SetActive(
                    sprite != null
                );
            }
        }

        // Cambiar el fondo segun el propietario.
        ActualizarFondoUnidad(unidad);

        if (!panelInfoUnidad.activeSelf)
            panelInfoUnidad.SetActive(true);

        // Vida actual y maxima.
        if (textoVida != null)
        {
            textoVida.text =
                unidad.CurrentHealth + "/" + unidad.maxHealth;
        }

        // Puntos de movimiento.
        if (textoMovimiento != null)
        {
            textoMovimiento.text =
                unidad.movementPoints.ToString();
        }

        // Estadistica de ataque.
        if (textoAtaque != null)
        {
            textoAtaque.text =
                unidad.attack.ToString();
        }
    }

    private void ActualizarFondoUnidad(AnimalUnit unidad)
    {
        if (imagenFondoUnidad == null || unidad == null)
            return;

        PlayerOwnership propietario =
            unidad.GetComponent<PlayerOwnership>();

        if (propietario == null)
            return;

        Sprite nuevoSprite = null;

        switch (propietario.playerId)
        {
            case 1:
                nuevoSprite = fondoJugador1;
                break;

            case 2:
                nuevoSprite = fondoJugador2;
                break;
        }

        if (nuevoSprite != null &&
            imagenFondoUnidad.sprite != nuevoSprite)
        {
            imagenFondoUnidad.sprite = nuevoSprite;
        }
    }

    private void DesactivarHoverAnterior()
    {
        if (anilloResaltado != null)
            anilloResaltado.SetHovered(false);

        anilloResaltado = null;
    }

    private void OcultarPanel()
    {
        DesactivarHoverAnterior();

        unidadMostrada = null;

        if (panelInfoUnidad != null &&
            panelInfoUnidad.activeSelf)
        {
            panelInfoUnidad.SetActive(false);
        }
    }

    private string GetAnimalName(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Jabali:
                return "Jabalí";

            case AnimalType.Tigre:
                return "Tigre";

            case AnimalType.Gorila:
                return "Gorila";

            case AnimalType.Cocodrilo:
                return "Cocodrilo";

            case AnimalType.Camello:
                return "Camello";

            default:
                return type.ToString();
        }
    }

    private Sprite GetPortrait(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Jabali:
                return retratoJabali;

            case AnimalType.Tigre:
                return retratoTigre;

            case AnimalType.Gorila:
                return retratoGorila;

            case AnimalType.Cocodrilo:
                return retratoCocodrilo;

            case AnimalType.Camello:
                return retratoCamello;

            default:
                return null;
        }
    }

    private bool IsInsideBoard(Vector3 point)
    {
        Vector3 center = gridManager.ground.position;

        float halfWidth =
            gridManager.width * gridManager.cellSize * 0.5f;

        float halfDepth =
            gridManager.depth * gridManager.cellSize * 0.5f;

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

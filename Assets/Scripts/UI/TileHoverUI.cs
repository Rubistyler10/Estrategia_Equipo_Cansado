using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(SpriteRenderer))]
public class TileHoverUI : MonoBehaviour
{
    [Header("Referencias")]
    public Camera gameCamera;
    public GridManager gridManager;
    public TMP_Text textoTerreno;

    [Header("Resaltado amarillo")]
    public float heightAboveNode = 1f;
    [Range(0.5f, 1f)]
    public float cellFill = 0.95f;

    [Header("Panel de informacion")]
    public GameObject panelInfoTerreno;

    [Header("GameObjects con los sprites de terreno")]
    public GameObject imagenLlanura;
    public GameObject imagenDesierto;
    public GameObject imagenNieve;
    public GameObject imagenAgua;

    private SpriteRenderer highlight;
    private GridNode currentNode;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        highlight = GetComponent<SpriteRenderer>();

        OcultarImagenes();
        HideHighlight();

        if (highlight.sprite == null)
        {
            Debug.LogError("Falta el sprite amarillo en TileHoverIndicator.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (gameCamera == null || gridManager == null ||
            gridManager.Grid == null || gridManager.ground == null)
        {
            HideHighlight();
            return;
        }

        // El panel informativo debe tener CanvasGroup con
        // Blocks Raycasts desactivado para no interceptar el cursor.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            HideHighlight();
            return;
        }

        Vector2 mousePosition = GetMousePosition();

        if (mousePosition.x < 0f || mousePosition.y < 0f ||
            mousePosition.x >= Screen.width ||
            mousePosition.y >= Screen.height)
        {
            HideHighlight();
            return;
        }

        Ray ray = gameCamera.ScreenPointToRay(mousePosition);
        Plane plane = new Plane(Vector3.up, gridManager.ground.position);

        if (!plane.Raycast(ray, out float distance))
        {
            HideHighlight();
            return;
        }

        Vector3 worldPosition = ray.GetPoint(distance);
        if (!IsInsideBoard(worldPosition))
        {
            HideHighlight();
            return;
        }

        GridNode node = gridManager.GetNodeFromWorldPosition(worldPosition);
        if (node == null)
        {
            HideHighlight();
            return;
        }

        ShowNode(node);
    }

    private void ShowNode(GridNode node)
    {
        if (highlight == null || highlight.sprite == null)
            return;

        highlight.enabled = true;
        transform.position = node.worldPosition + Vector3.up * heightAboveNode;

        float desiredSize = gridManager.cellSize * cellFill;
        Vector2 spriteSize = highlight.sprite.bounds.size;
        if (spriteSize.x > 0f && spriteSize.y > 0f)
        {
            transform.localScale = new Vector3(
                desiredSize / spriteSize.x,
                desiredSize / spriteSize.y,
                1f
            );
        }

        // Solo actualizar el texto y los GameObjects si cambia la casilla.
        if (currentNode != node)
        {
            currentNode = node;

            if (textoTerreno != null)
                textoTerreno.text = GetTerrainName(node.terrainType);

            MostrarImagenTerreno(node.terrainType);
        }

        if (panelInfoTerreno != null && !panelInfoTerreno.activeSelf)
            panelInfoTerreno.SetActive(true);
    }

    private void MostrarImagenTerreno(TerrainType tipo)
    {
        // Cada imagen conserva su sprite, RectTransform y escala originales.
        if (imagenLlanura != null)
            imagenLlanura.SetActive(tipo == TerrainType.Plain);

        if (imagenDesierto != null)
            imagenDesierto.SetActive(tipo == TerrainType.Desert);

        if (imagenNieve != null)
            imagenNieve.SetActive(tipo == TerrainType.Snow);

        if (imagenAgua != null)
            imagenAgua.SetActive(tipo == TerrainType.Water);
    }

    private void OcultarImagenes()
    {
        if (imagenLlanura != null) imagenLlanura.SetActive(false);
        if (imagenDesierto != null) imagenDesierto.SetActive(false);
        if (imagenNieve != null) imagenNieve.SetActive(false);
        if (imagenAgua != null) imagenAgua.SetActive(false);
    }

    private void HideHighlight()
    {
        if (highlight != null)
            highlight.enabled = false;

        if (panelInfoTerreno != null && panelInfoTerreno.activeSelf)
            panelInfoTerreno.SetActive(false);

        currentNode = null;
    }

    private bool IsInsideBoard(Vector3 point)
    {
        Vector3 center = gridManager.ground.position;
        float halfWidth = gridManager.width * gridManager.cellSize / 2f;
        float halfDepth = gridManager.depth * gridManager.cellSize / 2f;

        return point.x >= center.x - halfWidth &&
               point.x < center.x + halfWidth &&
               point.z >= center.z - halfDepth &&
               point.z < center.z + halfDepth;
    }

    private string GetTerrainName(TerrainType type)
    {
        switch (type)
        {
            case TerrainType.Plain: return "Llanura";
            case TerrainType.Desert: return "Desierto";
            case TerrainType.Snow: return "Nieve";
            case TerrainType.Water: return "Agua";
            default: return "Desconocido";
        }
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
        HideHighlight();
    }
}

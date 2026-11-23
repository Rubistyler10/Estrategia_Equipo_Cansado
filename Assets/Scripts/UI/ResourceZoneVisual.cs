
using UnityEngine;

[RequireComponent(typeof(ResourceZone))]
public class ResourceZoneVisual : MonoBehaviour
{
    [Header("Sprites de Nerea")]
    public Sprite spriteReposo;
    public Sprite spriteOcupado;

    [Header("Animacion")]
    [Min(0.1f)]
    public float intervalo = 0.5f;

    [Header("Posicion y tamaño")]
    [Range(0.5f, 1f)]
    public float cellFill = 0.90f;

    public float heightAboveNode = 0.06f;

    [Header("Renderizado")]
    public int sortingOrder = 5;

    [Header("Referencias")]
    public AnimalDragController dragController;

    private ResourceZone resourceZone;
    private SpriteRenderer spriteRenderer;
    private GridNode currentNode;

    private float temporizador;
    private bool segundoFotograma;

    private void Awake()
    {
        resourceZone = GetComponent<ResourceZone>();

        if (dragController == null)
            dragController =
                FindFirstObjectByType<AnimalDragController>();

        GameObject visual = new GameObject("IndicadorAlimento");
        visual.transform.SetParent(transform, false);

        spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.enabled = false;
    }

    private void Update()
    {
        if (resourceZone == null || spriteRenderer == null)
            return;

        GridNode node = resourceZone.GetNode();

        if (node == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        if (currentNode != node)
        {
            currentNode = node;
            temporizador = 0f;
            segundoFotograma = false;
        }

        bool ocupada = EstaOcupadaPorUnidad(node);

        if (ocupada)
        {
            temporizador += Time.deltaTime;

            if (temporizador >= intervalo)
            {
                temporizador %= intervalo;
                segundoFotograma = !segundoFotograma;
            }
        }
        else
        {
            temporizador = 0f;
            segundoFotograma = false;
        }

        Sprite sprite = ocupada && segundoFotograma
            ? spriteOcupado
            : spriteReposo;

        if (sprite == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        if (spriteRenderer.sprite != sprite)
            spriteRenderer.sprite = sprite;

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = sortingOrder;

        Transform visual = spriteRenderer.transform;

        visual.position =
            node.worldPosition + Vector3.up * heightAboveNode;

        visual.rotation = Quaternion.Euler(-90f, 0f, 0f);

        Vector2 spriteSize = sprite.bounds.size;

        if (spriteSize.x > 0f && spriteSize.y > 0f)
        {
            float size = ObtenerTamanoCasilla(node) * cellFill;

            visual.localScale = new Vector3(
                size / spriteSize.x,
                size / spriteSize.y,
                1f
            );
        }

        spriteRenderer.enabled = true;
    }

    private bool EstaOcupadaPorUnidad(GridNode node)
    {
        if (!node.isOccupied || node.occupant == null)
            return false;

        AnimalUnit unidad =
            node.occupant.GetComponent<AnimalUnit>();

        if (unidad == null || unidad.IsDead ||
            !unidad.isActiveAndEnabled)
            return false;

        PlayerOwnership propietario =
            unidad.GetComponent<PlayerOwnership>();

        if (propietario == null)
            return false;

        // No animar si la unidad esta levantada.
        if (dragController != null &&
            dragController.SelectedAnimal == unidad)
            return false;

        if (unidad.CurrentNode != node)
            return false;

        // Comprobar que el animal esta realmente
        // encima de la casilla y no en movimiento.
        Vector3 diferencia =
            unidad.transform.position - node.worldPosition;

        diferencia.y = 0f;

        float tolerancia = ObtenerTamanoCasilla(node) * 0.4f;

        return diferencia.sqrMagnitude <=
               tolerancia * tolerancia;
    }

    private float ObtenerTamanoCasilla(GridNode node)
    {
        GridManager grid =
            resourceZone.GetComponent<GridManager>();

        if (grid == null)
            grid = FindFirstObjectByType<GridManager>();

        return grid != null ? grid.cellSize : 1f;
    }

    private void OnDisable()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}

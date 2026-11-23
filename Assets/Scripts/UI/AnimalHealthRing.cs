using UnityEngine;

[RequireComponent(typeof(AnimalUnit))]
[RequireComponent(typeof(PlayerOwnership))]
public class AnimalHealthRing : MonoBehaviour
{
    [Header("Sprites de Nerea (opcional)")]
    public Sprite aroJugador1;
    public Sprite aroJugador2;
    public Sprite hoverJugador1;
    public Sprite hoverJugador2;

    [Range(0.5f, 1f)] public float spriteCellFill = 0.90f;
    public float spriteHeightOffset = 0.05f;
    public int spriteSortingOrder = 12;

    [Header("Anillo antiguo (respaldo sin sprites)")]
    [Range(0.2f, 0.5f)] public float radiusInCells = 0.42f;
    [Range(0.02f, 0.15f)] public float lineWidthInCells = 0.08f;
    public float heightOffset = 0.04f;
    public Color player1Color = new Color(0.1f, 0.55f, 1f);
    public Color player2Color = new Color(1f, 0.15f, 0.2f);
    public Color backgroundColor = new Color(0.12f, 0.12f, 0.12f);
    [Range(24, 128)] public int segments = 64;

    private AnimalUnit animal;
    private PlayerOwnership owner;
    private Transform ringRoot;
    private LineRenderer backgroundRing;
    private LineRenderer healthRing;
    private SpriteRenderer spriteRing;
    private Material ringMaterial;
    private bool hovered;
    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private int lastPlayer = -1;

    private void Awake()
    {
        animal = GetComponent<AnimalUnit>();
        owner = GetComponent<PlayerOwnership>();

        GameObject root = new GameObject("HealthRingVisual");
        root.transform.SetParent(transform, false);
        ringRoot = root.transform;

        GameObject spriteObject = new GameObject("NereaOwnerRing");
        spriteObject.transform.SetParent(ringRoot, false);
        spriteObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        spriteRing = spriteObject.AddComponent<SpriteRenderer>();
        spriteRing.sortingOrder = spriteSortingOrder;
        spriteRing.enabled = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("No se encontro Sprites/Default para el anillo antiguo.", this);
            return;
        }
        ringMaterial = new Material(shader);
        backgroundRing = CreateRing("BackgroundRing");
        healthRing = CreateRing("HealthRing");
        backgroundRing.sortingOrder = 0;
        healthRing.sortingOrder = 1;
        healthRing.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        backgroundRing.startColor = backgroundColor;
        backgroundRing.endColor = backgroundColor;
    }

    private LineRenderer CreateRing(string ringName)
    {
        GameObject obj = new GameObject(ringName);
        obj.transform.SetParent(ringRoot, false);
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = ringMaterial;
        line.useWorldSpace = false;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    // Lo llama UnitHoverUI cuando el raton entra o sale de la unidad.
    public void SetHovered(bool value)
    {
        hovered = value;
    }

    private void LateUpdate()
    {
        if (ringRoot == null) return;
        if (animal == null || animal.CurrentNode == null || animal.IsDead)
        {
            ringRoot.gameObject.SetActive(false);
            return;
        }
        ringRoot.gameObject.SetActive(true);

        float cellSize = animal.gridManager != null
            ? animal.gridManager.cellSize : 0.5f;
        int playerId = owner != null ? owner.playerId : 1;

        Sprite normal = playerId == 1 ? aroJugador1 : aroJugador2;
        Sprite hover = playerId == 1 ? hoverJugador1 : hoverJugador2;
        bool useSprites = normal != null;

        if (spriteRing != null)
        {
            spriteRing.enabled = useSprites;
            if (useSprites)
            {
                Sprite target = hovered && hover != null ? hover : normal;
                if (spriteRing.sprite != target) spriteRing.sprite = target;
                spriteRing.sortingOrder = spriteSortingOrder;
                spriteRing.transform.position = animal.CurrentNode.worldPosition +
                    Vector3.up * (animal.groundOffset + spriteHeightOffset);
                spriteRing.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                Vector2 size = target.bounds.size;
                float desiredSize = cellSize * spriteCellFill;
                if (size.x > 0f && size.y > 0f)
                    spriteRing.transform.localScale = new Vector3(
                        desiredSize / size.x, desiredSize / size.y, 1f);
            }
        }

        // En modo sprites no dibujamos los circulos viejos debajo.
        if (backgroundRing != null) backgroundRing.enabled = !useSprites;
        if (healthRing != null) healthRing.enabled = !useSprites;
        if (useSprites || backgroundRing == null || healthRing == null) return;

        float radius = cellSize * radiusInCells;
        backgroundRing.widthMultiplier = cellSize * lineWidthInCells;
        healthRing.widthMultiplier = cellSize * lineWidthInCells;
        float y = animal.CurrentNode.worldPosition.y + animal.groundOffset + heightOffset;
        ringRoot.position = new Vector3(transform.position.x, y, transform.position.z);

        if (lastHealth != animal.CurrentHealth ||
            lastMaxHealth != animal.maxHealth || lastPlayer != playerId)
        {
            Color color = playerId == 1 ? player1Color : player2Color;
            healthRing.startColor = color;
            healthRing.endColor = color;
            DrawArc(backgroundRing, radius, 1f);
            float healthPercent = Mathf.Clamp01(
                (float)animal.CurrentHealth / Mathf.Max(1, animal.maxHealth));
            DrawArc(healthRing, radius, healthPercent);
            lastHealth = animal.CurrentHealth;
            lastMaxHealth = animal.maxHealth;
            lastPlayer = playerId;
        }
    }

    private void DrawArc(LineRenderer line, float radius, float percentage)
    {
        if (percentage <= 0f)
        {
            line.positionCount = 0;
            return;
        }
        int steps = Mathf.Max(1, Mathf.CeilToInt(segments * percentage));
        line.positionCount = steps + 1;
        for (int i = 0; i <= steps; i++)
        {
            float progress = (float)i / steps * percentage;
            float angle = Mathf.PI * 0.5f - progress * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
    }
}

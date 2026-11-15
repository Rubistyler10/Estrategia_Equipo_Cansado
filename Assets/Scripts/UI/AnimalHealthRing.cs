
using UnityEngine;

[RequireComponent(typeof(AnimalUnit))]
[RequireComponent(typeof(PlayerOwnership))]
public class AnimalHealthRing : MonoBehaviour
{
    [Header("Tamano del anillo")]
    [Range(0.2f, 0.5f)]
    public float radiusInCells = 0.42f;

    [Range(0.02f, 0.15f)]
    public float lineWidthInCells = 0.08f;

    public float heightOffset = 0.04f;

    [Header("Colores")]
    public Color player1Color = new Color(0.1f, 0.55f, 1f);
    public Color player2Color = new Color(1f, 0.15f, 0.2f);
    public Color backgroundColor = new Color(0.12f, 0.12f, 0.12f);

    [Header("Calidad")]
    [Range(24, 128)]
    public int segments = 64;

    private AnimalUnit animal;
    private PlayerOwnership owner;

    private Transform ringRoot;
    private LineRenderer backgroundRing;
    private LineRenderer healthRing;

    private Material ringMaterial;

    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private int lastPlayer = -1;

    private void Awake()
    {
        animal = GetComponent<AnimalUnit>();
        owner = GetComponent<PlayerOwnership>();

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("No se encontro el shader para el anillo.");
            enabled = false;
            return;
        }

        ringMaterial = new Material(shader);

        GameObject root = new GameObject("HealthRingVisual");
        root.transform.SetParent(transform, false);
        ringRoot = root.transform;

        backgroundRing = CreateRing("BackgroundRing");
        healthRing = CreateRing("HealthRing");

        // Dibujar el anillo de vida por encima del fondo.
        backgroundRing.sortingOrder = 0;
        healthRing.sortingOrder = 1;

        // Evitar que ambos anillos se superpongan.
        healthRing.transform.localPosition =
        new Vector3(0f, 0.01f, 0f);

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

        line.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        line.receiveShadows = false;

        return line;
    }

    private void LateUpdate()
    {
        if (animal == null || animal.CurrentNode == null)
        {
            ringRoot.gameObject.SetActive(false);
            return;
        }

        if (animal.IsDead)
        {
            ringRoot.gameObject.SetActive(false);
            return;
        }

        ringRoot.gameObject.SetActive(true);

        float cellSize = animal.gridManager != null
            ? animal.gridManager.cellSize
            : 0.5f;

        float radius = cellSize * radiusInCells;

        backgroundRing.widthMultiplier =
            cellSize * lineWidthInCells;

        healthRing.widthMultiplier =
            cellSize * lineWidthInCells;

        // Se mantiene en el suelo incluso al levantar el animal.
        // Mantener el anillo a la altura de los pies.
        float y = animal.CurrentNode.worldPosition.y +
                animal.groundOffset +
                heightOffset;

        ringRoot.position = new Vector3(
            transform.position.x,
            y,
            transform.position.z
        );

        int playerId = owner != null ? owner.playerId : 1;

        if (lastHealth != animal.CurrentHealth ||
            lastMaxHealth != animal.maxHealth ||
            lastPlayer != playerId)
        {
            Color color = playerId == 1
                ? player1Color
                : player2Color;

            healthRing.startColor = color;
            healthRing.endColor = color;

            DrawArc(backgroundRing, radius, 1f);

            float healthPercent = Mathf.Clamp01(
                (float)animal.CurrentHealth /
                Mathf.Max(1, animal.maxHealth)
            );

            DrawArc(healthRing, radius, healthPercent);

            lastHealth = animal.CurrentHealth;
            lastMaxHealth = animal.maxHealth;
            lastPlayer = playerId;
        }
    }

    private void DrawArc(
        LineRenderer line,
        float radius,
        float percentage)
    {
        if (percentage <= 0f)
        {
            line.positionCount = 0;
            return;
        }

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(segments * percentage)
        );

        line.positionCount = steps + 1;

        for (int i = 0; i <= steps; i++)
        {
            float progress = (float)i / steps * percentage;

            float angle =
                Mathf.PI * 0.5f - progress * Mathf.PI * 2f;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            line.SetPosition(i, point);
        }
    }

    private void OnDestroy()
    {
        if (ringMaterial != null)
            Destroy(ringMaterial);
    }
}

using UnityEngine;

public enum InfluenceDisplayMode
{
    Both,
    Player1,
    Player2
}

public class InfluenceMap : MonoBehaviour
{
    public static bool SuppressOtherDebugGizmos { get; private set; }

    [Header("Referencias")]
    public GridManager gridManager;

    [Header("Caida de influencia")]
    [Min(0)] public int influenceRadiusInTiles = 5;

    [Header("Visualizacion de pruebas")]
    public bool showInfluenceGizmos = true;
    public bool exclusiveVisualization = true;
    public InfluenceDisplayMode displayMode = InfluenceDisplayMode.Both;
    public bool showInfluenceValues = true;
    [Min(0.01f)] public float maxVisibleInfluence = 5f;
    [Min(0f)] public float gizmoHeight = 0.15f;
    [Range(0.1f, 1f)] public float gizmoCellFill = 0.85f;
    [Tooltip("0 = opaco; 1 = totalmente transparente.")]
    [Range(0f, 1f)] public float influenceTransparency = 0.25f;
    public Color player1Color = new Color(0f, 0.2f, 1f);
    public Color player2Color = new Color(1f, 0f, 0f);
    public Color overlapColor = new Color(0.65f, 0f, 1f);

    private float[,] player1Influence;
    private float[,] player2Influence;
    private Camera gameCamera;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        gameCamera = Camera.main;
    }

    private void LateUpdate()
    {
        UpdateExclusiveVisualizationState();
        Recalculate();
    }

    private void OnEnable()
    {
        UpdateExclusiveVisualizationState();
    }

    private void OnValidate()
    {
        UpdateExclusiveVisualizationState();
    }

    private void OnDisable()
    {
        SuppressOtherDebugGizmos = false;
    }

    private void UpdateExclusiveVisualizationState()
    {
        SuppressOtherDebugGizmos =
            showInfluenceGizmos && exclusiveVisualization;
    }

    [ContextMenu("Recalcular mapa de influencia")]
    public void Recalculate()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        GridNode[,] grid = gridManager != null ? gridManager.Grid : null;
        if (grid == null)
            return;

        int width = grid.GetLength(0);
        int depth = grid.GetLength(1);
        if (player1Influence == null ||
            player1Influence.GetLength(0) != width ||
            player1Influence.GetLength(1) != depth)
        {
            player1Influence = new float[width, depth];
            player2Influence = new float[width, depth];
        }

        Clear(player1Influence);
        Clear(player2Influence);

        AnimalUnit[] units =
            FindObjectsByType<AnimalUnit>(FindObjectsSortMode.None);

        foreach (AnimalUnit unit in units)
        {
            if (unit == null || !unit.isActiveAndEnabled ||
                unit.IsDead || unit.CurrentHealth <= 0 ||
                unit.maxHealth <= 0 || unit.CurrentNode == null)
            {
                continue;
            }

            PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
            if (owner == null ||
                (owner.playerId != 1 && owner.playerId != 2))
            {
                continue;
            }

            float strength = (unit.attack + unit.defense) *
                (unit.CurrentHealth / (float)unit.maxHealth);
            AccumulateUnitInfluence(
                owner.playerId == 1
                    ? player1Influence
                    : player2Influence,
                unit.CurrentNode,
                strength,
                width,
                depth
            );
        }
    }

    public bool TryGetInfluence(
        int playerId, GridNode node, out float influence)
    {
        influence = 0f;
        if (gridManager == null || gridManager.Grid == null ||
            node == null || (playerId != 1 && playerId != 2))
        {
            return false;
        }

        GridNode[,] grid = gridManager.Grid;
        if (node.gridX < 0 || node.gridX >= grid.GetLength(0) ||
            node.gridZ < 0 || node.gridZ >= grid.GetLength(1) ||
            grid[node.gridX, node.gridZ] != node)
        {
            return false;
        }

        float[,] map = playerId == 1
            ? player1Influence
            : player2Influence;
        if (map == null)
            return false;

        influence = map[node.gridX, node.gridZ];
        return true;
    }

    private void AccumulateUnitInfluence(
        float[,] map,
        GridNode origin,
        float strength,
        int width,
        int depth)
    {
        if (strength <= 0f)
            return;

        int radius = Mathf.Max(0, influenceRadiusInTiles);
        int minX = Mathf.Max(0, origin.gridX - radius);
        int maxX = Mathf.Min(width - 1, origin.gridX + radius);
        int minZ = Mathf.Max(0, origin.gridZ - radius);
        int maxZ = Mathf.Min(depth - 1, origin.gridZ + radius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                int distance = Mathf.Max(
                    Mathf.Abs(x - origin.gridX),
                    Mathf.Abs(z - origin.gridZ)
                );
                if (distance > radius)
                    continue;

                float decay = radius == 0
                    ? 1f
                    : 1f - distance / (radius + 1f);
                map[x, z] += strength * decay;
            }
        }
    }

    private static void Clear(float[,] map)
    {
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int z = 0; z < map.GetLength(1); z++)
                map[x, z] = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showInfluenceGizmos || gridManager == null ||
            gridManager.Grid == null ||
            player1Influence == null || player2Influence == null)
        {
            return;
        }

        GridNode[,] grid = gridManager.Grid;
        float cellSize = gridManager.cellSize * gizmoCellFill;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                float player1 = player1Influence[x, z];
                float player2 = player2Influence[x, z];

                if (displayMode == InfluenceDisplayMode.Player1)
                    player2 = 0f;
                else if (displayMode == InfluenceDisplayMode.Player2)
                    player1 = 0f;

                if (player1 <= 0f && player2 <= 0f)
                    continue;

                bool hasPlayer1 = player1 > 0f;
                bool hasPlayer2 = player2 > 0f;
                Color baseColor = hasPlayer1 && hasPlayer2
                    ? overlapColor
                    : hasPlayer1 ? player1Color : player2Color;
                float magnitude = player1 + player2;
                float intensity = Mathf.Clamp01(
                    magnitude / Mathf.Max(0.01f, maxVisibleInfluence)
                );
                Color influenceColor = baseColor;
                influenceColor.a =
                    (1f - influenceTransparency) * intensity;

                Vector3 center = grid[x, z].worldPosition +
                    Vector3.up * gizmoHeight;
                Gizmos.color = influenceColor;
                Gizmos.DrawCube(center,
                    new Vector3(cellSize, 0.02f, cellSize));
            }
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !showInfluenceGizmos ||
            !showInfluenceValues || gridManager == null ||
            gridManager.Grid == null || gridManager.ground == null)
        {
            return;
        }

        if (gameCamera == null)
            gameCamera = Camera.main;
        if (gameCamera == null)
            return;

        Vector2 pointer = Event.current.mousePosition;
        Ray ray = gameCamera.ScreenPointToRay(
            new Vector3(pointer.x, Screen.height - pointer.y, 0f)
        );
        Plane boardPlane = new Plane(
            Vector3.up, gridManager.ground.position
        );
        if (!boardPlane.Raycast(ray, out float distance))
            return;

        Vector3 worldPosition = ray.GetPoint(distance);
        float halfWidth = gridManager.width * gridManager.cellSize / 2f;
        float halfDepth = gridManager.depth * gridManager.cellSize / 2f;
        Vector3 boardCenter = gridManager.ground.position;
        if (worldPosition.x < boardCenter.x - halfWidth ||
            worldPosition.x >= boardCenter.x + halfWidth ||
            worldPosition.z < boardCenter.z - halfDepth ||
            worldPosition.z >= boardCenter.z + halfDepth)
        {
            return;
        }

        GridNode node =
            gridManager.GetNodeFromWorldPosition(worldPosition);
        if (node == null ||
            !TryGetInfluence(1, node, out float player1) ||
            !TryGetInfluence(2, node, out float player2))
        {
            return;
        }

        string values = displayMode == InfluenceDisplayMode.Player1
            ? "P1: " + player1.ToString("0.00")
            : displayMode == InfluenceDisplayMode.Player2
                ? "P2: " + player2.ToString("0.00")
                : "P1: " + player1.ToString("0.00") +
                  "   P2: " + player2.ToString("0.00");

        const float boxWidth = 170f;
        const float boxHeight = 32f;
        float x = Mathf.Clamp(
            pointer.x + 14f, 0f, Screen.width - boxWidth
        );
        float y = Mathf.Clamp(
            pointer.y + 14f, 0f, Screen.height - boxHeight
        );
        GUI.Box(new Rect(x, y, boxWidth, boxHeight), values);
    }
}

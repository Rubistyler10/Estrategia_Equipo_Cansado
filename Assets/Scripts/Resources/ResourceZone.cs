
using UnityEngine;

public class ResourceZone : MonoBehaviour
{
    [Header("Referencias")]
    public GridManager gridManager;

    [Header("Configuracion")]
    public int foodPerTurn = 2;

    private void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        GridNode node = GetNode();

        if (node == null || !node.isWalkable)
        {
            Debug.LogWarning(
                "Zona de recoleccion invalida: " + name);
            return;
        }

        // Centramos la zona en su casilla.
        transform.position = node.worldPosition;
    }

    public GridNode GetNode()
    {
        if (gridManager == null)
            return null;

        return gridManager.GetNodeFromWorldPosition(
            transform.position);
    }

    public int GetOccupyingPlayer()
    {
        GridNode node = GetNode();

        if (node == null || !node.isWalkable ||
            !node.isOccupied || node.occupant == null)
            return 0;

        AnimalUnit animal =
            node.occupant.GetComponent<AnimalUnit>();

        PlayerOwnership owner =
            node.occupant.GetComponent<PlayerOwnership>();

        if (animal == null || owner == null ||
            !animal.isActiveAndEnabled)
            return 0;

        if (animal.CurrentNode != node)
            return 0;

        return owner.playerId;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.75f);

        Vector3 position = transform.position;

        if (Application.isPlaying && gridManager != null)
        {
            GridNode node = GetNode();

            if (node != null)
                position = node.worldPosition;
        }

        float size = gridManager != null
            ? gridManager.cellSize * 0.8f
            : 0.4f;

        Gizmos.DrawWireCube(
            position,
            new Vector3(size, 0.15f, size));

        Gizmos.DrawSphere(
            position + Vector3.up * 0.3f,
            size * 0.15f);
    }
}

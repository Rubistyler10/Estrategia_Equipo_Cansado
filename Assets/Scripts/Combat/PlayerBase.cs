using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    // These values are serialized so each base can be configured independently
    // from the Unity Inspector.
    [Header("Base")]
    [Range(1, 2)] public int playerId = 1;
    [Min(1)] public int maxHealth = 30;
    public GridManager gridManager;

    public GridNode CurrentNode { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDestroyed { get; private set; }

    private void Start()
    {
        // Health is initialized at runtime so changing maxHealth in the
        // Inspector also changes the starting health.
        CurrentHealth = maxHealth;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null || gridManager.Grid == null)
        {
            Debug.LogError("[PLAYERBASE][START] Falta GridManager en " + name + ".");
            enabled = false;
            return;
        }

        GridNode node =
            gridManager.GetNodeFromWorldPosition(transform.position);

        // A base is a normal grid occupant. Invalid placement disables the
        // component instead of leaving an unregistered base in the game.
        if (node == null ||
            !gridManager.OccupyNode(node.gridX, node.gridZ, gameObject))
        {
            Debug.LogError("[PLAYERBASE][START] Casilla inicial invalida: " +
                name + ".");
            enabled = false;
            return;
        }

        CurrentNode = node;
        Debug.Log("[PLAYERBASE][START] Base del jugador " + playerId +
            " creada con " + CurrentHealth + " HP.");
    }

    public void ReceiveDamage(int damage)
    {
        // Clamp both incoming damage and the resulting health to avoid
        // negative values from invalid callers or overkill.
        if (IsDestroyed)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage));
        Debug.Log("[PLAYERBASE][DAMAGE] " + name + " recibe " + damage +
            " dano. HP restante: " + CurrentHealth + ".");
    }

    public void DestroyBase()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;

        // Free the occupied tile before disabling the object so units can
        // use the tile after the base has been defeated.
        if (gridManager != null && CurrentNode != null)
        {
            gridManager.FreeNode(
                CurrentNode.gridX,
                CurrentNode.gridZ,
                gameObject
            );
        }

        foreach (Collider baseCollider in GetComponentsInChildren<Collider>())
            baseCollider.enabled = false;

        // Keep the defeated base out of gameplay while preserving the object
        // long enough for other systems to finish their current call.
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // This is a safety net for destruction paths other than DestroyBase.
        if (gridManager != null && CurrentNode != null)
        {
            gridManager.FreeNode(
                CurrentNode.gridX,
                CurrentNode.gridZ,
                gameObject
            );
        }
    }
}

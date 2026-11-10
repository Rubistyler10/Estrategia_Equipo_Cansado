using UnityEngine;

public class PlayerBase : MonoBehaviour
{
    [Header("Base")]
    [Range(1, 2)] public int playerId = 1;
    [Min(1)] public int maxHealth = 30;
    public GridManager gridManager;

    public GridNode CurrentNode { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDestroyed { get; private set; }

    private void Start()
    {
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

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
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

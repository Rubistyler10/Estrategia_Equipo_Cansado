using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public TurnManager turnManager;

    private void Awake()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
    }

    public bool TryAttack(AnimalUnit attacker, AnimalUnit defender)
    {
        if (attacker == null || defender == null ||
            attacker == defender || !attacker.CanAttack())
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACK] Ataque rechazado: atacante invalido.");
            return false;
        }

        PlayerOwnership attackerOwner =
            attacker.GetComponent<PlayerOwnership>();
        PlayerOwnership defenderOwner =
            defender.GetComponent<PlayerOwnership>();

        if (attackerOwner == null || defenderOwner == null ||
            attackerOwner.playerId == defenderOwner.playerId ||
            !IsInRange(attacker, defender))
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACK] Ataque rechazado: objetivo fuera de rango o aliado.");
            return false;
        }

        int damage = Mathf.Max(0, attacker.attack - defender.defense);
        defender.ReceiveDamage(damage);
        attacker.MarkAttacked();
        attacker.PlayAttackBump(defender.transform.position);

        Debug.Log("[COMBATSYSTEM][TRYATTACK] Ataque realizado: " +
            attacker.name + " -> " + defender.name + " (" + damage + " de dano).");

        if (defender.IsDefeated())
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACK] Unidad derrotada: " + defender.name);
            defender.Die();
        }

        return true;
    }

    public bool TryAttack(AnimalUnit attacker, PlayerBase defender)
    {
        if (attacker == null || defender == null ||
            !attacker.CanAttack() || defender.IsDestroyed)
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACKBASE] Ataque rechazado: " +
                "atacante o base invalida.");
            return false;
        }

        PlayerOwnership attackerOwner =
            attacker.GetComponent<PlayerOwnership>();

        if (attackerOwner == null ||
            attackerOwner.playerId == defender.playerId ||
            !IsInRange(attacker, defender.CurrentNode))
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACKBASE] Ataque rechazado: " +
                "base aliada o fuera de rango.");
            return false;
        }

        int damage = Mathf.Max(0, attacker.attack);
        defender.ReceiveDamage(damage);
        attacker.MarkAttacked();
        attacker.PlayAttackBump(defender.transform.position);

        Debug.Log("[COMBATSYSTEM][TRYATTACKBASE] " + attacker.name +
            " ataca la base del jugador " + defender.playerId + ".");

        if (defender.CurrentHealth <= 0)
        {
            defender.DestroyBase();
            if (turnManager != null)
                turnManager.EndGame(attackerOwner.playerId);
        }

        return true;
    }

    private bool IsInRange(AnimalUnit attacker, AnimalUnit defender)
    {
        if (attacker.CurrentNode == null || defender.CurrentNode == null)
            return false;

        return IsInRange(attacker, defender.CurrentNode);
    }

    private bool IsInRange(AnimalUnit attacker, GridNode defenderNode)
    {
        if (attacker.CurrentNode == null || defenderNode == null)
            return false;

        int xDistance =
            Mathf.Abs(attacker.CurrentNode.gridX - defenderNode.gridX);
        int zDistance =
            Mathf.Abs(attacker.CurrentNode.gridZ - defenderNode.gridZ);

        return Mathf.Max(xDistance, zDistance) <= attacker.attackRange;
    }
}

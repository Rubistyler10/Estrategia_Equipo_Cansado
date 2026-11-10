using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    // The turn manager is used to stop the match as soon as a base is destroyed.
    public TurnManager turnManager;

    private void Awake()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
    }

    public bool TryAttack(AnimalUnit attacker, AnimalUnit defender)
    {
        // A unit must still have its attack action available before it can
        // target another unit.
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

        // Units can only attack opposing players and must be within the
        // attacker's Chebyshev grid range (diagonal tiles count as one step).
        if (attackerOwner == null || defenderOwner == null ||
            attackerOwner.playerId == defenderOwner.playerId ||
            !IsInRange(attacker, defender))
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACK] Ataque rechazado: objetivo fuera de rango o aliado.");
            return false;
        }

        int damage = Mathf.Max(0, attacker.attack - defender.defense);
        defender.ReceiveDamage(damage);
        // Marking the action before playing the visual effect prevents a
        // second attack from being accepted during the bump animation.
        attacker.MarkAttacked();
        attacker.PlayAttackBump(defender.transform.position);

        Debug.Log("[COMBATSYSTEM][TRYATTACK] Ataque realizado: " +
            attacker.name + " -> " + defender.name + " (" + damage + " de dano).");

        if (defender.IsDefeated())
        {
            // Defeat removes the unit from the grid immediately, then lets
            // its death animation finish before destroying its GameObject.
            Debug.Log("[COMBATSYSTEM][TRYATTACK] Unidad derrotada: " + defender.name);
            defender.Die();
        }

        return true;
    }

    public bool TryAttack(AnimalUnit attacker, PlayerBase defender)
    {
        // Bases use a separate overload because they have health and
        // ownership, but no AnimalUnit combat stats of their own.
        if (attacker == null || defender == null ||
            !attacker.CanAttack() || defender.IsDestroyed)
        {
            Debug.Log("[COMBATSYSTEM][TRYATTACKBASE] Ataque rechazado: " +
                "atacante o base invalida.");
            return false;
        }

        PlayerOwnership attackerOwner =
            attacker.GetComponent<PlayerOwnership>();

        // A base cannot be attacked by its owner and must occupy a node in
        // the attacker's range.
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
            // Destroying the opposing base is the match-winning condition.
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

        // Chebyshev distance matches the eight-direction movement/range
        // convention used by the board.
        int xDistance =
            Mathf.Abs(attacker.CurrentNode.gridX - defenderNode.gridX);
        int zDistance =
            Mathf.Abs(attacker.CurrentNode.gridZ - defenderNode.gridZ);

        return Mathf.Max(xDistance, zDistance) <= attacker.attackRange;
    }
}

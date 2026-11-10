using UnityEngine;

public abstract class PlayerTurnAgent : MonoBehaviour
{
    // AI and other automated players implement this hook. The manager calls
    // it after resetting the active player's actions and UI.
    public abstract void PlayTurn(TurnManager turnManager);
}

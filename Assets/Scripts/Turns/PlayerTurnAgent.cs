using UnityEngine;

public abstract class PlayerTurnAgent : MonoBehaviour
{
    public abstract void PlayTurn(TurnManager turnManager);
}

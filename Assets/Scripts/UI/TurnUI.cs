using TMPro;
using UnityEngine;

public class TurnUI : MonoBehaviour
{
    [Header("Sistemas existentes")]
    public TurnManager turnManager;
    public ResourceManager resourceManager;

    [Header("Textos del HUD")]
    public TMP_Text textoTurno;
    public TMP_Text numeroTurno;
    public TMP_Text textoJugador;
    public TMP_Text textoAlimentos;

    private void Awake()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (resourceManager == null)
            resourceManager = FindFirstObjectByType<ResourceManager>();
    }

    private void LateUpdate()
    {
        if (turnManager == null)
            return;

        int jugador = turnManager.currentPlayerId;

        // Texto fijo y numero independiente para usar tamaños de fuente diferentes.
        if (textoTurno != null)
            textoTurno.text = "Turno";

        if (numeroTurno != null)
            numeroTurno.text = turnManager.turnNumber.ToString();

        if (textoJugador != null)
        {
            textoJugador.text = turnManager.IsGameOver
                ? "Jugador " + turnManager.WinningPlayerId + " gana"
                : "Jugador " + jugador;
        }

        if (textoAlimentos != null && resourceManager != null)
        {
        textoAlimentos.text =
            resourceManager.GetFood(jugador).ToString();
        }
    }
}

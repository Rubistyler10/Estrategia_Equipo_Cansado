
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Encabezado del jugador")]
    public Image imagenEncabezadoJugador;

    [Header("Sprites de los jugadores")]
    public Sprite encabezadoJugador1;
    public Sprite encabezadoJugador2;

    private int ultimoJugador = -1;

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

        // Texto fijo del turno.
        if (textoTurno != null)
            textoTurno.text = "Turno";

        // Numero de turno.
        if (numeroTurno != null)
            numeroTurno.text = turnManager.turnNumber.ToString();

        // Nombre del jugador activo.
        if (textoJugador != null)
        {
            textoJugador.text = turnManager.IsGameOver
                ? "Jugador " + turnManager.WinningPlayerId + " gana"
                : "Jugador " + jugador;
        }

        // Mostrar solamente la cantidad de alimentos.
        if (textoAlimentos != null && resourceManager != null)
        {
            textoAlimentos.text =
                resourceManager.GetFood(jugador).ToString();
        }

        // Cambiar el encabezado cuando cambia el jugador.
        if (ultimoJugador != jugador)
        {
            ActualizarEncabezado(jugador);
            ultimoJugador = jugador;
        }
    }

    private void ActualizarEncabezado(int jugador)
    {
        if (imagenEncabezadoJugador == null)
            return;

        Sprite nuevoSprite = jugador == 1
            ? encabezadoJugador1
            : encabezadoJugador2;

        if (nuevoSprite != null &&
            imagenEncabezadoJugador.sprite != nuevoSprite)
        {
            imagenEncabezadoJugador.sprite = nuevoSprite;
        }
    }
}

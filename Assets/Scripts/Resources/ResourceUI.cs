
using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    [Header("Referencias")]
    public ResourceManager resourceManager;
    public TextMeshProUGUI textoAlimentos;

    [Header("Jugador activo")]
    [Range(1, 2)]
    public int activePlayer = 1;

    private int lastPlayer = -1;
    private int lastFood = -1;

    private void Start()
    {
        if (resourceManager == null)
            resourceManager =
                FindFirstObjectByType<ResourceManager>();

        UpdateUI();
    }

    private void Update()
    {
        if (resourceManager == null)
            return;

        int food = resourceManager.GetFood(activePlayer);

        if (lastPlayer != activePlayer ||
            lastFood != food)
        {
            UpdateUI();
        }
    }

    public void SetActivePlayer(int playerId)
    {
        if (playerId != 1 && playerId != 2)
            return;

        activePlayer = playerId;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (resourceManager == null ||
            textoAlimentos == null)
            return;

        int food = resourceManager.GetFood(activePlayer);

        textoAlimentos.text =
            "Jugador " + activePlayer +
            " - Alimentos: " + food;

        lastPlayer = activePlayer;
        lastFood = food;
    }

    [ContextMenu("Mostrar jugador 1")]
    public void TestPlayer1()
    {
        SetActivePlayer(1);
    }

    [ContextMenu("Mostrar jugador 2")]
    public void TestPlayer2()
    {
        SetActivePlayer(2);
    }
}

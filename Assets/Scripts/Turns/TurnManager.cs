using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnManager : MonoBehaviour
{
    [Header("Referencias")]
    public ResourceManager resourceManager;
    public UnitSpawner unitSpawner;
    public ResourceUI resourceUI;
    public Button endTurnButton;
    public TMP_Text currentPlayerText;

    [Header("Control opcional por IA")]
    public PlayerTurnAgent player1AI;
    public PlayerTurnAgent player2AI;

    [Header("Estado")]
    [Range(1, 2)] public int currentPlayerId = 1;
    public int turnNumber = 1;
    public bool IsGameOver { get; private set; }
    public int WinningPlayerId { get; private set; }

    private bool collectedThisTurn;

    private void Awake()
    {
        if (resourceManager == null)
            resourceManager = FindFirstObjectByType<ResourceManager>();
        if (unitSpawner == null)
            unitSpawner = FindFirstObjectByType<UnitSpawner>();
        if (resourceUI == null)
            resourceUI = FindFirstObjectByType<ResourceUI>();
    }

    private void Start()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(EndTurn);

        BeginTurn(currentPlayerId);
    }

    public bool IsPlayersTurn(int playerId)
    {
        return playerId == currentPlayerId;
    }

    public bool CanUnitAct(AnimalUnit unit)
    {
        if (unit == null)
            return false;

        PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
        return owner != null && IsPlayersTurn(owner.playerId);
    }

    public void EndTurn()
    {
        if (IsGameOver)
            return;

        CollectResourcesIfNeeded();

        currentPlayerId = currentPlayerId == 1 ? 2 : 1;
        if (currentPlayerId == 1)
            turnNumber++;

        BeginTurn(currentPlayerId);
    }

    public void BeginTurn(int playerId)
    {
        if (IsGameOver)
            return;

        currentPlayerId = playerId;
        collectedThisTurn = false;
        UpdateTurnUI();

        AnimalUnit[] units =
            FindObjectsByType<AnimalUnit>(FindObjectsSortMode.None);

        foreach (AnimalUnit unit in units)
        {
            PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
            if (owner != null && owner.playerId == currentPlayerId)
                unit.ResetTurnActions();
        }

        if (unitSpawner != null)
            unitSpawner.StartPlayerTurn(currentPlayerId);

        if (resourceUI != null)
            resourceUI.SetActivePlayer(currentPlayerId);

        PlayerTurnAgent ai = currentPlayerId == 1 ? player1AI : player2AI;
        if (ai != null)
            ai.PlayTurn(this);
    }

    public void EndGame(int winningPlayerId)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        WinningPlayerId = winningPlayerId;

        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(false);

        if (currentPlayerText != null)
        {
            currentPlayerText.text =
                "Jugador " + winningPlayerId + " gana";
        }

        Debug.Log("[TURNMANAGER][ENDGAME] Gana el jugador " +
            winningPlayerId + ".");
    }

    private void UpdateTurnUI()
    {
        PlayerTurnAgent ai = currentPlayerId == 1 ? player1AI : player2AI;

        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(ai == null);

        if (currentPlayerText != null)
        {
            currentPlayerText.text =
                "Jugador actual: " + currentPlayerId;
        }
        Debug.Log("[TURNMANAGER][UPDATETURNUI] Turno del jugador " +
            currentPlayerId + ".");
    }

    private void CollectResourcesIfNeeded()
    {
        if (collectedThisTurn || resourceManager == null)
            return;

        AnimalUnit[] units =
            FindObjectsByType<AnimalUnit>(FindObjectsSortMode.None);

        foreach (AnimalUnit unit in units)
        {
            PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
            if (owner == null || owner.playerId != currentPlayerId ||
                unit.CurrentNode == null)
                continue;

            ResourceZone[] zones =
                FindObjectsByType<ResourceZone>(FindObjectsSortMode.None);

            foreach (ResourceZone zone in zones)
            {
                if (zone.GetNode() == unit.CurrentNode)
                {
                    resourceManager.CollectResources(currentPlayerId);
                    collectedThisTurn = true;
                    return;
                }
            }
        }
    }
}


using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitCreationUI : MonoBehaviour
{
    [Header("Referencias")]
    public UnitSpawner unitSpawner;
    public ResourceManager resourceManager;
    public ResourceUI resourceUI;

    // Prevent this player's recruitment controls from being used during the
    // opposing player's turn.
    public TurnManager turnManager;

    [Header("Panel")]
    public HUDSwitcher hudSwitcher;
    public Button botonAbrir;
    public Button botonCerrar;

    [Header("Botones de animales")]
    public Button botonJabali;
    public Button botonTigre;
    public Button botonGorila;
    public Button botonCocodrilo;
    public Button botonCamello;

    private void Start()
    {
        botonAbrir.onClick.AddListener(AbrirPanel);
        botonCerrar.onClick.AddListener(CerrarPanel);

        botonJabali.onClick.AddListener(
            () => CrearAnimal(AnimalType.Jabali));

        botonTigre.onClick.AddListener(
            () => CrearAnimal(AnimalType.Tigre));

        botonGorila.onClick.AddListener(
            () => CrearAnimal(AnimalType.Gorila));

        botonCocodrilo.onClick.AddListener(
            () => CrearAnimal(AnimalType.Cocodrilo));

        botonCamello.onClick.AddListener(
            () => CrearAnimal(AnimalType.Camello));
    }

    private void Update()
    {
        if (hudSwitcher == null ||
            hudSwitcher.hudReclutarUnidad == null ||
            !hudSwitcher.hudReclutarUnidad.activeSelf)
            return;

        int playerId = resourceUI.activePlayer;

        // The panel may remain open across a turn change.
        if (turnManager != null &&
            !turnManager.IsPlayersTurn(playerId))
        {
            CerrarPanel();
            return;
        }

        int food = resourceManager.GetFood(playerId);

        botonJabali.interactable =
            food >= unitSpawner.costeJabali;

        botonTigre.interactable =
            food >= unitSpawner.costeTigre;

        botonGorila.interactable =
            food >= unitSpawner.costeGorila;

        botonCocodrilo.interactable =
            food >= unitSpawner.costeCocodrilo;

        botonCamello.interactable =
            food >= unitSpawner.costeCamello;
    }

    public void AbrirPanel()
    {
        if (hudSwitcher == null)
            return;

        hudSwitcher.MostrarHUDReclutar();
    }

    public void CerrarPanel()
    {
        if (hudSwitcher == null)
            return;

        hudSwitcher.MostrarHUDJugar();
    }

    private void CrearAnimal(AnimalType tipo)
    {
        int playerId = resourceUI.activePlayer;

        // Check again at the action boundary in case a queued UI click
        // arrives after the turn has changed.
        if (turnManager != null &&
            !turnManager.IsPlayersTurn(playerId))
            return;

        bool creado =
            unitSpawner.CreateUnit(playerId, tipo);

        if (creado)
        {
            Debug.Log("Unidad creada correctamente.");
            CerrarPanel();
        }
    }
}
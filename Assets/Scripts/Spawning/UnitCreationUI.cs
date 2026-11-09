
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitCreationUI : MonoBehaviour
{
    [Header("Referencias")]
    public UnitSpawner unitSpawner;
    public ResourceManager resourceManager;
    public ResourceUI resourceUI;

    [Header("Panel")]
    public GameObject panelCrearUnidad;
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

        panelCrearUnidad.SetActive(false);
    }

    private void Update()
    {
        if (!panelCrearUnidad.activeSelf)
            return;

        int playerId = resourceUI.activePlayer;
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
        panelCrearUnidad.SetActive(true);
    }

    public void CerrarPanel()
    {
        panelCrearUnidad.SetActive(false);
    }

    private void CrearAnimal(AnimalType tipo)
    {
        int playerId = resourceUI.activePlayer;

        bool creado =
            unitSpawner.CreateUnit(playerId, tipo);

        if (creado)
        {
            Debug.Log("Unidad creada correctamente.");
            CerrarPanel();
        }
    }
}

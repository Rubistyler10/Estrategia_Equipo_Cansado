
using UnityEngine;
using UnityEngine.UI;

public class UnitCreationUI : MonoBehaviour
{
    [Header("Referencias")]
    public UnitSpawner unitSpawner;
    public ResourceManager resourceManager;
    public ResourceUI resourceUI;
    public TurnManager turnManager;

    [Header("Panel")]
    public HUDSwitcher hudSwitcher;
    public Button botonAbrir;
    public Button botonCerrar;

    [Header("Botones de animales (click invisible)")]
    public Button botonJabali;
    public Button botonTigre;
    public Button botonGorila;
    public Button botonCocodrilo;
    public Button botonCamello;

    [Header("Imagenes visibles")]
    public Image imagenJabali;
    public Image imagenTigre;
    public Image imagenGorila;
    public Image imagenCocodrilo;
    public Image imagenCamello;

    [Header("Sprites disponibles (SI)")]
    public Sprite jabaliSI;
    public Sprite tigreSI;
    public Sprite gorilaSI;
    public Sprite cocodriloSI;
    public Sprite camelloSI;

    [Header("Sprites no disponibles (NO)")]
    public Sprite jabaliNO;
    public Sprite tigreNO;
    public Sprite gorilaNO;
    public Sprite cocodriloNO;
    public Sprite camelloNO;

    private void Start()
    {
        if (hudSwitcher == null)
            hudSwitcher = GetComponent<HUDSwitcher>();

        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (botonAbrir != null)
            botonAbrir.onClick.AddListener(AbrirPanel);

        if (botonCerrar != null)
            botonCerrar.onClick.AddListener(CerrarPanel);

        if (botonJabali != null)
            botonJabali.onClick.AddListener(
                () => CrearAnimal(AnimalType.Jabali));

        if (botonTigre != null)
            botonTigre.onClick.AddListener(
                () => CrearAnimal(AnimalType.Tigre));

        if (botonGorila != null)
            botonGorila.onClick.AddListener(
                () => CrearAnimal(AnimalType.Gorila));

        if (botonCocodrilo != null)
            botonCocodrilo.onClick.AddListener(
                () => CrearAnimal(AnimalType.Cocodrilo));

        if (botonCamello != null)
            botonCamello.onClick.AddListener(
                () => CrearAnimal(AnimalType.Camello));
    }

    private void Update()
    {
        if (hudSwitcher == null ||
            hudSwitcher.hudReclutarUnidad == null ||
            !hudSwitcher.hudReclutarUnidad.activeSelf)
            return;

        if (resourceUI == null ||
            resourceManager == null ||
            unitSpawner == null)
            return;

        int playerId = resourceUI.activePlayer;

        // Cerrar el menu si cambia el turno.
        if (turnManager != null &&
            !turnManager.IsPlayersTurn(playerId))
        {
            CerrarPanel();
            return;
        }

        ActualizarBotones(playerId);
    }

    private void ActualizarBotones(int playerId)
    {
        int food = resourceManager.GetFood(playerId);

        ActualizarAnimal(
            botonJabali,
            imagenJabali,
            jabaliSI,
            jabaliNO,
            food >= unitSpawner.costeJabali
        );

        ActualizarAnimal(
            botonTigre,
            imagenTigre,
            tigreSI,
            tigreNO,
            food >= unitSpawner.costeTigre
        );

        ActualizarAnimal(
            botonGorila,
            imagenGorila,
            gorilaSI,
            gorilaNO,
            food >= unitSpawner.costeGorila
        );

        ActualizarAnimal(
            botonCocodrilo,
            imagenCocodrilo,
            cocodriloSI,
            cocodriloNO,
            food >= unitSpawner.costeCocodrilo
        );

        ActualizarAnimal(
            botonCamello,
            imagenCamello,
            camelloSI,
            camelloNO,
            food >= unitSpawner.costeCamello
        );
    }

    private void ActualizarAnimal(
        Button boton,
        Image imagen,
        Sprite spriteSI,
        Sprite spriteNO,
        bool disponible)
    {
        // Activar o desactivar la compra.
        if (boton != null)
            boton.interactable = disponible;

        // Cambiar solo la imagen visible.
        if (imagen != null)
        {
            Sprite nuevoSprite =
                disponible ? spriteSI : spriteNO;

            if (nuevoSprite != null &&
                imagen.sprite != nuevoSprite)
            {
                imagen.sprite = nuevoSprite;
            }
        }
    }

    public void AbrirPanel()
    {
        if (hudSwitcher == null ||
            resourceUI == null ||
            resourceManager == null ||
            unitSpawner == null)
            return;

        int playerId = resourceUI.activePlayer;

        if (turnManager != null &&
            !turnManager.IsPlayersTurn(playerId))
            return;

        // Actualizar antes de mostrar el menu.
        ActualizarBotones(playerId);

        hudSwitcher.MostrarHUDReclutar();
    }

    public void CerrarPanel()
    {
        if (hudSwitcher != null)
            hudSwitcher.MostrarHUDJugar();
    }

    private void CrearAnimal(AnimalType tipo)
    {
        if (resourceUI == null ||
            resourceManager == null ||
            unitSpawner == null)
            return;

        int playerId = resourceUI.activePlayer;

        if (turnManager != null &&
            !turnManager.IsPlayersTurn(playerId))
            return;

        bool creado = unitSpawner.CreateUnit(playerId, tipo);

        if (creado)
        {
            Debug.Log("Unidad creada correctamente.");
            CerrarPanel();
        }
        else
        {
            // Refrescar si la compra no se completa.
            ActualizarBotones(playerId);
        }
    }
}

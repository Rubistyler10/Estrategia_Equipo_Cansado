
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GridManager gridManager;
    public ResourceManager resourceManager;

    [Header("Puntos de aparicion")]
    public Transform spawnJugador1;
    public Transform spawnJugador2;

    [Header("Prefabs")]
    public GameObject jabaliPrefab;
    public GameObject tigrePrefab;
    public GameObject gorilaPrefab;
    public GameObject cocodriloPrefab;
    public GameObject camelloPrefab;

    [Header("Costes")]
    public int costeJabali = 4;
    public int costeTigre = 6;
    public int costeGorila = 8;
    public int costeCocodrilo = 8;
    public int costeCamello = 10;

    private bool creoJugador1 = false;
    private bool creoJugador2 = false;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (resourceManager == null)
            resourceManager =
                FindFirstObjectByType<ResourceManager>();
    }

    public bool CreateUnit(int playerId, AnimalType type)
    {
        if (gridManager == null || resourceManager == null)
            return false;

        if (playerId != 1 && playerId != 2)
            return false;

        if ((playerId == 1 && creoJugador1) ||
            (playerId == 2 && creoJugador2))
        {
            Debug.Log("Ya has creado una unidad este turno.");
            return false;
        }

        GameObject prefab = GetPrefab(type);
        int cost = GetCost(type);

        if (prefab == null || cost < 0)
        {
            Debug.LogWarning("Prefab o coste no valido.");
            return false;
        }

        if (resourceManager.GetFood(playerId) < cost)
        {
            Debug.Log("Alimentos insuficientes.");
            return false;
        }

        Transform spawn = playerId == 1
            ? spawnJugador1 : spawnJugador2;

        if (spawn == null)
        {
            Debug.LogWarning("Falta el punto de aparicion.");
            return false;
        }

        GridNode node = FindFreeNode(spawn.position, type);

        if (node == null)
        {
            Debug.Log("No hay casillas libres junto a la base.");
            return false;
        }

        AnimalUnit prefabUnit =
            prefab.GetComponent<AnimalUnit>();

        PlayerOwnership prefabOwner =
            prefab.GetComponent<PlayerOwnership>();

        if (prefabUnit == null || prefabOwner == null)
        {
            Debug.LogError(
                "El prefab necesita AnimalUnit y PlayerOwnership.");
            return false;
        }

        // La altura final la ajusta AnimalUnit.Start().
        GameObject unit = Instantiate(
            prefab,
            node.worldPosition,
            prefab.transform.rotation
        );

        unit.SetActive(false);

        AnimalUnit animal = unit.GetComponent<AnimalUnit>();
        PlayerOwnership owner =
            unit.GetComponent<PlayerOwnership>();

        owner.playerId = playerId;
        animal.gridManager = gridManager;

        // El animal registrara su casilla en Start().
        unit.SetActive(true);

        if (playerId == 1)
            creoJugador1 = true;
        else
            creoJugador2 = true;

        resourceManager.SpendFood(playerId, cost);

        Debug.Log("Jugador " + playerId +
            " crea " + type + " por " + cost + " alimentos.");

        return true;
    }

    private GridNode FindFreeNode(
        Vector3 position, AnimalType type)
    {
        if (gridManager.Grid == null ||
            gridManager.ground == null)
            return null;

        float startX = gridManager.ground.position.x -
            gridManager.width * gridManager.cellSize / 2f;

        float startZ = gridManager.ground.position.z -
            gridManager.depth * gridManager.cellSize / 2f;

        int x = Mathf.FloorToInt(
            (position.x - startX) / gridManager.cellSize);

        int z = Mathf.FloorToInt(
            (position.z - startZ) / gridManager.cellSize);

        // Primero comprobamos la casilla indicada
        // y despues las ocho casillas vecinas.
        for (int radius = 0; radius <= 1; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    GridNode node =
                        gridManager.GetNode(x + dx, z + dz);

                    if (node == null ||
                        !node.isWalkable ||
                        node.isOccupied)
                        continue;

                    // Evitamos crear animales terrestres
                    // directamente en el agua.
                    if (node.terrainType == TerrainType.Water &&
                        type != AnimalType.Cocodrilo)
                        continue;

                    return node;
                }
            }
        }

        return null;
    }

    private GameObject GetPrefab(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Jabali: return jabaliPrefab;
            case AnimalType.Tigre: return tigrePrefab;
            case AnimalType.Gorila: return gorilaPrefab;
            case AnimalType.Cocodrilo: return cocodriloPrefab;
            case AnimalType.Camello: return camelloPrefab;
            default: return null;
        }
    }

    private int GetCost(AnimalType type)
    {
        switch (type)
        {
            case AnimalType.Jabali: return costeJabali;
            case AnimalType.Tigre: return costeTigre;
            case AnimalType.Gorila: return costeGorila;
            case AnimalType.Cocodrilo: return costeCocodrilo;
            case AnimalType.Camello: return costeCamello;
            default: return -1;
        }
    }

    // Ruben llamara a este metodo al comenzar
    // el turno del jugador correspondiente.
    public void StartPlayerTurn(int playerId)
    {
        if (playerId == 1)
            creoJugador1 = false;
        else if (playerId == 2)
            creoJugador2 = false;
    }

    [ContextMenu("Prueba: crear jabali jugador 1")]
    public void TestCreateJabali()
    {
        CreateUnit(1, AnimalType.Jabali);
    }

    [ContextMenu("Prueba: crear tigre jugador 2")]
    public void TestCreateTigre()
    {
        CreateUnit(2, AnimalType.Tigre);
    }

    [ContextMenu("Prueba: nuevo turno ambos jugadores")]
    public void TestNewTurn()
    {
        StartPlayerTurn(1);
        StartPlayerTurn(2);
    }
}

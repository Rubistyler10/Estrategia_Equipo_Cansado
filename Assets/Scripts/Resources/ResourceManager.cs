
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Alimentos de cada jugador")]
    public int alimentosJugador1 = 0;
    public int alimentosJugador2 = 0;

    [Header("Generacion de alimentos")]
    public int baseFoodPerTurn = 2;

    // Ruben llamara a este metodo al comenzar
    // el turno de un jugador.
    public void CollectResources(int playerId)
    {
        if (playerId != 1 && playerId != 2)
        {
            Debug.LogWarning("Jugador no valido.");
            return;
        }

        int foodEarned = baseFoodPerTurn;

        ResourceZone[] zones =
            FindObjectsByType<ResourceZone>(
                FindObjectsSortMode.None);

        foreach (ResourceZone zone in zones)
        {
            if (zone.GetOccupyingPlayer() == playerId)
            {
                foodEarned += zone.foodPerTurn;
            }
        }

        if (playerId == 1)
        {
            alimentosJugador1 += foodEarned;
            Debug.Log("Jugador 1: +" + foodEarned +
                " alimentos. Total: " + alimentosJugador1);
        }
        else
        {
            alimentosJugador2 += foodEarned;
            Debug.Log("Jugador 2: +" + foodEarned +
                " alimentos. Total: " + alimentosJugador2);
        }
    }

    public int GetFood(int playerId)
    {
        if (playerId == 1)
            return alimentosJugador1;

        if (playerId == 2)
            return alimentosJugador2;

        return 0;
    }

    // Preparado para futuros costes de unidades.
    public bool SpendFood(int playerId, int amount)
    {
        if (amount < 0 || GetFood(playerId) < amount)
            return false;

        if (playerId == 1)
            alimentosJugador1 -= amount;
        else if (playerId == 2)
            alimentosJugador2 -= amount;
        else
            return false;

        return true;
    }

    // Pruebas provisionales sin sistema de turnos.
    [ContextMenu("Probar turno jugador 1")]
    public void TestPlayer1()
    {
        CollectResources(1);
    }

    [ContextMenu("Probar turno jugador 2")]
    public void TestPlayer2()
    {
        CollectResources(2);
    }
}

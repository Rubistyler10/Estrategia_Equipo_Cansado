using UnityEngine;

public class AutonomousMovementTest : MonoBehaviour
{
    public AnimalUnit unit;
    public GridManager gridManager;
    public Transform objective;

    [ContextMenu("Probar movimiento autónomo")]
    private void TestMovement()
    {
        if (unit == null || gridManager == null || objective == null)
        {
            Debug.LogError("Asigna la unidad, el GridManager y el objetivo.");
            return;
        }

        GridNode target = gridManager.GetNodeFromWorldPosition(objective.position);
        if (target == null)
        {
            Debug.LogError("No se encontró una casilla para el objetivo.");
            return;
        }

        bool started = unit.MoveTowardsObjective(target);
        Debug.Log(started
            ? "La unidad ha comenzado a moverse."
            : "No se movió: revisa turno, movimiento disponible y ruta.");
    }
}
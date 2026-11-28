
using System;
using UnityEngine;

/// <summary>
/// IA Global de Wild Wars (Nerea).
/// Decide una prioridad por turno y publica objetivos en la Blackboard
/// existente de Rubén. No mueve, recluta ni finaliza turnos.
/// </summary>
public class GlobalAIController : MonoBehaviour
{
    [Header("Referencias existentes")]
    public TurnManager turnManager;
    public GridManager gridManager;
    public InfluenceMap influenceMap;

    [Header("Configuracion de IA Global")]
    [Range(1, 2)] public int aiPlayerId = 2;
    [Min(1)] public int baseDangerDistance = 3;
    [Min(0f)] public float baseDangerInfluence = 3f;
    [Min(0f)] public float resourceDangerWeight = 2f;
    public bool evaluateAutomatically = true;
    public bool showDecisionLogs = true;

    [Header("Prioridades (cuanto mayor, mas importante)")]
    [Min(1)] public int defendPriority = 100;
    [Min(1)] public int focusThreatPriority = 90;
    [Min(1)] public int capturePriority = 70;
    [Min(1)] public int attackPriority = 60;
    [Min(1)] public int aggressionPriority = 40;

    public enum StrategicDecision { None, Defend, Capture, Attack }
    public StrategicDecision LastDecision { get; private set; }
    public Blackboard SharedBlackboard { get; private set; }
    public event Action<Blackboard> ObjectivesUpdated;

    private BehaviourTask root;
    private PlayerBase ownBase;
    private PlayerBase enemyBase;
    private AnimalUnit closestThreat;
    private GridNode bestResourceNode;
    private bool baseThreatened;
    private int evaluatedTurn = int.MinValue;
    private int evaluatedPlayer = -1;

    private void Awake()
    {
        ResolveReferences();
        SharedBlackboard = new Blackboard(influenceMap, aiPlayerId);

        // Arbol determinista: la primera rama valida gana.
        // Defender > conquistar recursos > atacar la base enemiga.
        root = new Selector(
            new Sequencer(new BaseInDanger(this), new DefendBase(this)),
            new Sequencer(new ResourceAvailable(this), new CaptureResource(this)),
            new AttackEnemyBase(this)
        );
        root.BindBlackboard(SharedBlackboard);
    }

    private void Update()
    {
        if (!evaluateAutomatically || turnManager == null ||
            turnManager.IsGameOver || turnManager.currentPlayerId != aiPlayerId)
            return;

        // Evaluar solo una vez por turno, no en cada frame.
        if (evaluatedTurn == turnManager.turnNumber &&
            evaluatedPlayer == turnManager.currentPlayerId)
            return;

        EvaluateNow();
    }

    [ContextMenu("Evaluar IA Global ahora")]
    public void EvaluateNow()
    {
        ResolveReferences();
        if (turnManager == null || turnManager.IsGameOver ||
            turnManager.currentPlayerId != aiPlayerId ||
            gridManager == null || gridManager.Grid == null)
            return;

        if (!ReadGameState())
            return; // Esperar a que las bases/unidades terminen su Start().

        SharedBlackboard.ClearObjectives();
        LastDecision = StrategicDecision.None;

        BehaviourTask.TerminationType result = root.Run();
        if (result != BehaviourTask.TerminationType.SUCCESS)
        {
            Debug.LogWarning("[IA GLOBAL] No se ha encontrado un objetivo valido.");
            return;
        }

        evaluatedTurn = turnManager.turnNumber;
        evaluatedPlayer = turnManager.currentPlayerId;

        if (showDecisionLogs)
        {
            Debug.Log("[IA GLOBAL] Jugador " + aiPlayerId +
                " | Turno " + evaluatedTurn +
                " | Decision: " + LastDecision +
                " | Objetivos: " + SharedBlackboard.GetAllObjectives().Count);
            LogObjectives();
        }

        // Tomas puede subscribirse cuando conecte la IA Estrategica.
        ObjectivesUpdated?.Invoke(SharedBlackboard);
    }

    [ContextMenu("Mostrar objetivos de IA Global")]
    public void LogObjectives()
    {
        if (SharedBlackboard == null) return;
        foreach (var entry in SharedBlackboard.GetAllObjectives())
        {
            Blackboard.ObjectiveData goal = entry.Value;
            string target = goal.targetTile != null
                ? "casilla (" + goal.targetTile.gridX + ", " +
                  goal.targetTile.gridZ + ")"
                : goal.targetUnit != null ? goal.targetUnit.name : "general";
            Debug.Log("[IA GLOBAL] " + entry.Key +
                " | prioridad " + goal.priority + " | " + target);
        }
    }

    private void ResolveReferences()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
        if (influenceMap == null)
            influenceMap = FindFirstObjectByType<InfluenceMap>();
    }

    private bool ReadGameState()
    {
        ownBase = null;
        enemyBase = null;
        closestThreat = null;
        bestResourceNode = null;
        baseThreatened = false;

        foreach (PlayerBase playerBase in
                 FindObjectsByType<PlayerBase>(FindObjectsSortMode.None))
        {
            if (!playerBase.isActiveAndEnabled || playerBase.IsDestroyed ||
                playerBase.CurrentNode == null)
                continue;

            if (playerBase.playerId == aiPlayerId) ownBase = playerBase;
            else enemyBase = playerBase;
        }
        if (ownBase == null || enemyBase == null)
            return false;

        // El mapa puede estar desactualizado hasta LateUpdate: actualizarlo
        // solo una vez en esta evaluacion, no por cada posible objetivo.
        if (influenceMap != null)
            influenceMap.Recalculate();

        int enemyId = aiPlayerId == 1 ? 2 : 1;
        int closestDistance = int.MaxValue;
        AnimalUnit[] allUnits =
            FindObjectsByType<AnimalUnit>(FindObjectsSortMode.None);

        foreach (AnimalUnit unit in allUnits)
        {
            if (!IsLivingUnit(unit)) continue;
            PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
            if (owner == null || owner.playerId != enemyId) continue;

            int distance = TileDistance(ownBase.CurrentNode, unit.CurrentNode);
            if (closestThreat == null || distance < closestDistance ||
                (distance == closestDistance &&
                 ComesBefore(unit.CurrentNode, closestThreat.CurrentNode)))
            {
                closestDistance = distance;
                closestThreat = unit;
            }
        }

        baseThreatened = closestThreat != null &&
                         closestDistance <= baseDangerDistance;

        if (influenceMap != null &&
            influenceMap.TryGetInfluence(enemyId, ownBase.CurrentNode,
                                        out float danger) &&
            danger >= baseDangerInfluence && danger > 0f)
        {
            baseThreatened = true;
        }

        // Elegir una zona NO ocupada por nosotros ni por el rival.
        // La distancia es una aproximacion estrategica, no un pathfinding.
        float bestScore = float.PositiveInfinity;
        foreach (ResourceZone zone in
                 FindObjectsByType<ResourceZone>(FindObjectsSortMode.None))
        {
            GridNode node = zone.GetNode();
            if (node == null || !node.isWalkable || node.isOccupied ||
                zone.GetOccupyingPlayer() != 0)
                continue;

            int distance = TileDistance(ownBase.CurrentNode, node);
            bool anyUnitCanEnter = false;
            foreach (AnimalUnit unit in allUnits)
            {
                if (!IsLivingUnit(unit)) continue;
                PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
                if (owner == null || owner.playerId != aiPlayerId) continue;
                if (unit.GetTerrainCost(node.terrainType) == int.MaxValue)
                    continue;

                anyUnitCanEnter = true;
                distance = Mathf.Min(distance,
                    TileDistance(unit.CurrentNode, node));
            }
            // Si aun no quedan animales, la estrategia puede necesitar
            // reclutar; usamos la base como origen aproximado.
            if (!anyUnitCanEnter && node.terrainType == TerrainType.Water)
                continue;

            float enemyInfluence = 0f;
            if (influenceMap != null)
                influenceMap.TryGetInfluence(enemyId, node, out enemyInfluence);

            float score = distance +
                          resourceDangerWeight * Mathf.Max(0f, enemyInfluence);
            if (bestResourceNode == null || score < bestScore - 0.0001f ||
                (Mathf.Abs(score - bestScore) <= 0.0001f &&
                 ComesBefore(node, bestResourceNode)))
            {
                bestScore = score;
                bestResourceNode = node;
            }
        }
        return true;
    }

    private static bool IsLivingUnit(AnimalUnit unit)
    {
        return unit != null && unit.isActiveAndEnabled && !unit.IsDead &&
               unit.CurrentHealth > 0 && unit.CurrentNode != null;
    }

    private static int TileDistance(GridNode a, GridNode b)
    {
        return Mathf.Max(Mathf.Abs(a.gridX - b.gridX),
                         Mathf.Abs(a.gridZ - b.gridZ));
    }

    private static bool ComesBefore(GridNode a, GridNode b)
    {
        return a.gridX < b.gridX ||
               (a.gridX == b.gridX && a.gridZ < b.gridZ);
    }

    // TAREAS DEL ARBOL: condiciones y acciones separadas.
    private sealed class BaseInDanger : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public BaseInDanger(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        {
            return ai.baseThreatened ? TerminationType.SUCCESS :
                                       TerminationType.FAILURE;
        }
    }

    private sealed class ResourceAvailable : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public ResourceAvailable(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        {
            return ai.bestResourceNode != null ? TerminationType.SUCCESS :
                                                 TerminationType.FAILURE;
        }
    }

    private sealed class DefendBase : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public DefendBase(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        {
            if (blackboard == null || ai.ownBase == null) return TerminationType.FAILURE;
            int turn = ai.turnManager.turnNumber;
            blackboard.SetObjective(Blackboard.ObjectiveTypes.IncreaseDefence,
                ai.defendPriority, 1, turn, ai.ownBase.CurrentNode,
                targetAreaRadius: ai.baseDangerDistance, overrideExisting: true);
            if (ai.closestThreat != null)
                blackboard.SetObjective(Blackboard.ObjectiveTypes.FocusEnemyUnit,
                    ai.focusThreatPriority, 1, turn,
                    targetUnit: ai.closestThreat, overrideExisting: true);
            ai.LastDecision = StrategicDecision.Defend;
            return TerminationType.SUCCESS;
        }
    }

    private sealed class CaptureResource : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public CaptureResource(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        {
            if (blackboard == null || ai.bestResourceNode == null)
                return TerminationType.FAILURE;
            blackboard.SetObjective(Blackboard.ObjectiveTypes.ControlArea,
                ai.capturePriority, 1, ai.turnManager.turnNumber,
                targetTile: ai.bestResourceNode, targetAreaRadius: 0,
                overrideExisting: true);
            ai.LastDecision = StrategicDecision.Capture;
            return TerminationType.SUCCESS;
        }
    }

    private sealed class AttackEnemyBase : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public AttackEnemyBase(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        {
            if (blackboard == null || ai.enemyBase == null)
                return TerminationType.FAILURE;
            int turn = ai.turnManager.turnNumber;
            blackboard.SetObjective(Blackboard.ObjectiveTypes.FocusTile,
                ai.attackPriority, 1, turn,
                targetTile: ai.enemyBase.CurrentNode, overrideExisting: true);
            blackboard.SetObjective(Blackboard.ObjectiveTypes.IncreaseAggression,
                ai.aggressionPriority, 1, turn, overrideExisting: true);
            ai.LastDecision = StrategicDecision.Attack;
            return TerminationType.SUCCESS;
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// IA Global de Wild Wars (Nerea): publica objetivos simultaneos.
/// No mueve, recluta, ataca ni finaliza turnos.
/// Usa Blackboard y BehaviourTask del equipo.
/// </summary>
public class GlobalAIController : MonoBehaviour
{
    [Header("Referencias")]
    public TurnManager turnManager;
    public GridManager gridManager;
    public InfluenceMap influenceMap;
    public ResourceManager resourceManager;

    [Header("IA Global")]
    [Range(1, 2)] public int aiPlayerId = 2;
    public bool evaluateAutomatically = true;
    public bool showDecisionLogs = true;

    [Header("Defensa preventiva")]
    [Min(1)] public int baseAlertDistance = 8;
    [Min(1)] public int baseEmergencyDistance = 4;
    [Min(0f)] public float baseDangerInfluence = 3f;

    [Header("Recursos: solo si compensan")]
    [Min(0)] public int maxDistanceToResource = 5;
    [Min(0)] public int maxDetourToResource = 3;
    [Min(0f)] public float maxEnemyInfluenceOnResource = 4f;
    [Min(0f)] public float resourceDangerWeight = 2f;
    [Min(0)] public int lowFoodThreshold = 7;

    [Header("Necesidad de refuerzos")]
    [Min(1)] public int desiredArmySize = 5;
    [Min(0)] public int cheapestRecruitCost = 4;

    [Header("Pesos para futura personalidad (Ruben)")]
    [Range(0.5f, 2f)] public float defenseWeight = 1f;
    [Range(0.5f, 2f)] public float attackWeight = 1f;
    [Range(0.5f, 2f)] public float resourceWeight = 1f;
    [Range(0.5f, 2f)] public float expansionWeight = 1f;

    // Ruben podra cambiar estos pesos sin tocar el arbol ni la Blackboard.
    // No introducimos azar aqui: el WeightedSelector del equipo aun no esta implementado.
    public void SetPersonalityWeights(float defense, float attack,
        float resource, float expansion)
    {
        defenseWeight = Mathf.Clamp(defense, 0.5f, 2f);
        attackWeight = Mathf.Clamp(attack, 0.5f, 2f);
        resourceWeight = Mathf.Clamp(resource, 0.5f, 2f);
        expansionWeight = Mathf.Clamp(expansion, 0.5f, 2f);
    }

    private static int Weighted(int priority, float weight)
    {
        return Mathf.Clamp(Mathf.RoundToInt(priority * weight), 1, 200);
    }

    public enum StrategicDecision { None, Defend, Capture, Attack, PrepareDefense }
    public StrategicDecision LastDecision { get; private set; }
    public bool NeedsReinforcements { get; private set; }
    public bool CanAffordRecruitment { get; private set; }
    public Blackboard SharedBlackboard { get; private set; }
    public event Action<Blackboard> ObjectivesUpdated;

    private BehaviourTask root;
    private PlayerBase ownBase;
    private PlayerBase enemyBase;
    private AnimalUnit closestThreat;
    private GridNode bestResourceNode;
    private bool dangerCritical;
    private bool dangerAlert;
    private int friendlyCount;
    private int enemyCount;
    private int food;
    private int evaluatedTurn = int.MinValue;
    private int evaluatedPlayer = -1;

    private void Awake()
    {
        ResolveReferences();
        SharedBlackboard = new Blackboard(influenceMap, aiPlayerId);

        // Un solo arbol. Cada rama publica varias ordenes compatibles.
        root = new Selector(
            new Sequencer(new IsCritical(this), new PublishCritical(this)),
            new Sequencer(new IsOnAlert(this), new PublishAlert(this)),
            new PublishNormal(this)
        );
        root.BindBlackboard(SharedBlackboard);
    }

    private void OnValidate()
    {
        if (baseAlertDistance < baseEmergencyDistance)
            baseAlertDistance = baseEmergencyDistance;
    }

    private void Update()
    {
        if (!evaluateAutomatically || turnManager == null ||
            turnManager.IsGameOver || turnManager.currentPlayerId != aiPlayerId)
            return;

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

        if (!ReadGameState()) return;

        SharedBlackboard.ClearObjectives();
        LastDecision = StrategicDecision.None;
        BehaviourTask.TerminationType result = root.Run();
        if (result != BehaviourTask.TerminationType.SUCCESS) return;

        evaluatedTurn = turnManager.turnNumber;
        evaluatedPlayer = turnManager.currentPlayerId;

        if (showDecisionLogs)
        {
            Debug.Log("[IA GLOBAL] Jugador " + aiPlayerId + " | Turno " + evaluatedTurn +
                " | Decision: " + LastDecision +
                " | Aliados: " + friendlyCount + " | Rivales: " + enemyCount +
                " | Alimentos: " + food +
                " | Reclutar recomendado: " + NeedsReinforcements +
                " | Objetivos: " + SharedBlackboard.GetAllObjectives().Count);
            LogObjectives();
        }
        ObjectivesUpdated?.Invoke(SharedBlackboard);
    }

    [ContextMenu("Mostrar objetivos actuales")]
    public void LogObjectives()
    {
        if (SharedBlackboard == null) return;
        foreach (var item in SharedBlackboard.GetAllObjectives())
        {
            Blackboard.ObjectiveData data = item.Value;
            string target = data.targetTile != null
                ? "casilla (" + data.targetTile.gridX + ", " + data.targetTile.gridZ + ")"
                : data.targetUnit != null ? data.targetUnit.name : "general";
            Debug.Log("[IA GLOBAL] " + item.Key + " | prioridad " +
                data.priority + " | " + target);
        }
    }

    private void ResolveReferences()
    {
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (influenceMap == null) influenceMap = FindFirstObjectByType<InfluenceMap>();
        if (resourceManager == null) resourceManager = FindFirstObjectByType<ResourceManager>();
    }

    private bool ReadGameState()
    {
        ownBase = null;
        enemyBase = null;
        closestThreat = null;
        bestResourceNode = null;
        friendlyCount = 0;
        enemyCount = 0;
        dangerCritical = false;
        dangerAlert = false;
        int enemyId = aiPlayerId == 1 ? 2 : 1;

        foreach (PlayerBase baseObject in FindObjectsByType<PlayerBase>(FindObjectsSortMode.None))
        {
            if (!baseObject.isActiveAndEnabled || baseObject.IsDestroyed ||
                baseObject.CurrentNode == null) continue;
            if (baseObject.playerId == aiPlayerId) ownBase = baseObject;
            else if (baseObject.playerId == enemyId) enemyBase = baseObject;
        }
        if (ownBase == null || enemyBase == null) return false;

        // El InfluenceMap original recalcula en LateUpdate. Aqui se pide el
        // estado actualizado una sola vez para esta evaluacion.
        if (influenceMap != null) influenceMap.Recalculate();

        AnimalUnit[] allUnits = FindObjectsByType<AnimalUnit>(FindObjectsSortMode.None);
        int closestDistance = int.MaxValue;
        foreach (AnimalUnit unit in allUnits)
        {
            if (!IsLivingUnit(unit)) continue;
            PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
            if (owner == null) continue;
            if (owner.playerId == aiPlayerId) { friendlyCount++; continue; }
            if (owner.playerId != enemyId) continue;
            enemyCount++;

            int distance = Distance(ownBase.CurrentNode, unit.CurrentNode);
            if (closestThreat == null || distance < closestDistance ||
                (distance == closestDistance && ComesBefore(unit.CurrentNode, closestThreat.CurrentNode)))
            {
                closestThreat = unit;
                closestDistance = distance;
            }
        }

        dangerAlert = closestThreat != null && closestDistance <= baseAlertDistance;
        dangerCritical = closestThreat != null && closestDistance <= baseEmergencyDistance;
        if (influenceMap != null && influenceMap.TryGetInfluence(
            enemyId, ownBase.CurrentNode, out float influence) &&
            influence >= baseDangerInfluence && influence > 0f)
        {
            dangerCritical = true;
            dangerAlert = true;
        }

        food = resourceManager != null ? resourceManager.GetFood(aiPlayerId) : 0;
        NeedsReinforcements = friendlyCount < desiredArmySize ||
                              enemyCount >= friendlyCount + 2;
        CanAffordRecruitment = resourceManager != null && food >= cheapestRecruitCost;

        // Buscar solo alimento cercano y de camino a la base enemiga.
        // Es una puntuacion aproximada (no comprueba la ruta exacta).
        float lowestScore = float.PositiveInfinity;
        foreach (ResourceZone zone in FindObjectsByType<ResourceZone>(FindObjectsSortMode.None))
        {
            GridNode node = zone.GetNode();
            if (node == null || !node.isWalkable || node.isOccupied ||
                zone.GetOccupyingPlayer() != 0) continue;

            float enemyInfluence = 0f;
            if (influenceMap != null)
                influenceMap.TryGetInfluence(enemyId, node, out enemyInfluence);
            if (enemyInfluence > maxEnemyInfluenceOnResource) continue;

            foreach (AnimalUnit unit in allUnits)
            {
                if (!IsLivingUnit(unit)) continue;
                PlayerOwnership owner = unit.GetComponent<PlayerOwnership>();
                if (owner == null || owner.playerId != aiPlayerId ||
                    unit.GetTerrainCost(node.terrainType) == int.MaxValue) continue;

                int distanceToResource = Distance(unit.CurrentNode, node);
                if (distanceToResource > maxDistanceToResource) continue;

                int detour = distanceToResource + Distance(node, enemyBase.CurrentNode)
                           - Distance(unit.CurrentNode, enemyBase.CurrentNode);
                if (detour > maxDetourToResource) continue;

                float score = distanceToResource + detour * 2f +
                              Mathf.Max(0f, enemyInfluence) * resourceDangerWeight;
                if (bestResourceNode == null || score < lowestScore - 0.001f ||
                    (Mathf.Abs(score - lowestScore) <= 0.001f &&
                     ComesBefore(node, bestResourceNode)))
                {
                    bestResourceNode = node;
                    lowestScore = score;
                }
            }
        }
        return true;
    }

    private void PublishOrders(int defencePriority, int threatPriority,
        int attackPriority, int capturePriority, int expansionPriority)
    {
        int turn = turnManager.turnNumber;
        // Base rival: SIEMPRE presente, aunque existan alimentos sin capturar.
        SharedBlackboard.SetObjective(Blackboard.ObjectiveTypes.FocusTile,
            Weighted(attackPriority, attackWeight), 1, turn, targetTile: enemyBase.CurrentNode,
            overrideExisting: true);
        SharedBlackboard.SetObjective(Blackboard.ObjectiveTypes.IncreaseAggression,
            Weighted(Mathf.Max(1, attackPriority - 30), attackWeight), 1, turn,
            overrideExisting: true);

        if (defencePriority > 0)
        {
            // En emergencia no permitimos que una personalidad agresiva
            // convierta la defensa obligatoria de la base en secundaria.
            int finalDefence = Weighted(defencePriority, defenseWeight);
            if (dangerCritical) finalDefence = Mathf.Max(150, finalDefence);
            SharedBlackboard.SetObjective(Blackboard.ObjectiveTypes.IncreaseDefence,
                finalDefence, 1, turn, targetTile: ownBase.CurrentNode,
                targetAreaRadius: baseAlertDistance, overrideExisting: true);
            if (closestThreat != null)
                SharedBlackboard.SetObjective(Blackboard.ObjectiveTypes.FocusEnemyUnit,
                    dangerCritical ? Mathf.Max(135, Weighted(threatPriority, defenseWeight))
                                   : Weighted(threatPriority, defenseWeight),
                    1, turn, targetUnit: closestThreat,
                    overrideExisting: true);
        }

        if (bestResourceNode != null)
        {
            int priority = food < lowFoodThreshold
                ? Mathf.Min(90, capturePriority + 25) : capturePriority;
            priority = Weighted(priority, resourceWeight);
            if (dangerCritical) priority = Mathf.Min(priority, 100);
            SharedBlackboard.SetObjective(Blackboard.ObjectiveTypes.ControlArea,
                priority, 1, turn, targetTile: bestResourceNode,
                overrideExisting: true);
        }

        // ExpandInfluence expresa necesidad de incrementar presencia propia.
        // No es una orden literal de reclutar. La IA Estrategica decidira
        // si puede pagar y crear una unidad, usando NeedsReinforcements.
        if (NeedsReinforcements)
        {
            int priority = friendlyCount == 0 ? 110 :
                (CanAffordRecruitment ? Mathf.Max(expansionPriority, 85) : expansionPriority);
            priority = Weighted(priority, expansionWeight);
            if (dangerCritical) priority = Mathf.Min(priority, 120);
            SharedBlackboard.SetObjective(Blackboard.ObjectiveTypes.ExpandInfluence,
                priority, 1, turn, targetTile: ownBase.CurrentNode,
                overrideExisting: true);
        }
    }

    private void MakeCriticalPlan()
    {
        PublishOrders(100, 90, 40, 20, 70);
        LastDecision = StrategicDecision.Defend;
    }

    private void MakeAlertPlan()
    {
        PublishOrders(80, 70, 65, 50, 75);
        LastDecision = StrategicDecision.PrepareDefense;
    }

    private void MakeNormalPlan()
    {
        PublishOrders(0, 0, 80, 60, 70);
        LastDecision = bestResourceNode != null && food < lowFoodThreshold
            ? StrategicDecision.Capture : StrategicDecision.Attack;
    }

    private static bool IsLivingUnit(AnimalUnit unit) =>
        unit != null && unit.isActiveAndEnabled && !unit.IsDead &&
        unit.CurrentHealth > 0 && unit.CurrentNode != null;

    private static int Distance(GridNode a, GridNode b) =>
        Mathf.Max(Mathf.Abs(a.gridX - b.gridX), Mathf.Abs(a.gridZ - b.gridZ));

    private static bool ComesBefore(GridNode a, GridNode b) =>
        a.gridX < b.gridX || (a.gridX == b.gridX && a.gridZ < b.gridZ);

    private sealed class IsCritical : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public IsCritical(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run() => ai.dangerCritical
            ? TerminationType.SUCCESS : TerminationType.FAILURE;
    }
    private sealed class IsOnAlert : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public IsOnAlert(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run() => ai.dangerAlert
            ? TerminationType.SUCCESS : TerminationType.FAILURE;
    }
    private sealed class PublishCritical : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public PublishCritical(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        { ai.MakeCriticalPlan(); return TerminationType.SUCCESS; }
    }
    private sealed class PublishAlert : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public PublishAlert(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        { ai.MakeAlertPlan(); return TerminationType.SUCCESS; }
    }
    private sealed class PublishNormal : BehaviourTask
    {
        private readonly GlobalAIController ai;
        public PublishNormal(GlobalAIController ai) { this.ai = ai; }
        public override TerminationType Run()
        { ai.MakeNormalPlan(); return TerminationType.SUCCESS; }
    }
}

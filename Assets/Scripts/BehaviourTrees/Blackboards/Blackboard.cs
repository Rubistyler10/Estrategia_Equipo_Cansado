using System.Collections.Generic;
using UnityEngine;

public class Blackboard
{
    InfluenceMap influenceMap;
    int playerId;
    public enum ObjectiveTypes
    {
        IncreaseAggression,
        DecreaseAggression,
        IncreaseDefence,
        ControlArea,
        FocusTile,
        FocusEnemyUnit,
        ExpandInfluence,
        DefendAllyUnit,

    }

    public struct ObjectiveData
    {
        public int priority {get; private set;}
        public int turnsToExpiration {get; private set;}
        public int turnOfCreation {get; private set;}
        public GridNode targetTile {get; private set;}
        public int targetAreaRadius {get; private set;}
        public AnimalUnit targetUnit {get; private set;}

        public ObjectiveData(
            int priority,
            int turnsToExpiration,
            int turnOfCreation,
            GridNode targetTile = null,
            int targetAreaRadius = 0,
            AnimalUnit targetUnit = null
        )
        {
            this.priority = priority;
            this.turnsToExpiration = turnsToExpiration;
            this.turnOfCreation = turnOfCreation;
            this.targetTile = targetTile;
            this.targetAreaRadius = targetAreaRadius;
            this.targetUnit = targetUnit;
        }
    }

    Dictionary<ObjectiveTypes, ObjectiveData> objectives;

    public Blackboard(InfluenceMap influenceMap, int playerId)
    {
        this.influenceMap = influenceMap;
        this.playerId = playerId;
        objectives = new();
    }

    /// <summary>
    /// Adds an objective to the blackboard or updates an existing one. 
    /// If an objective of the same type already exists, 
    /// it will be replaced if the new objective has a higher priority
    /// or if overrideExisting is set to true.
    /// </summary>
    public void SetObjective(
        ObjectiveTypes type,
        int priority,
        int turnsToExpiration,
        int turnOfCreation,
        GridNode targetTile = null,
        int targetAreaRadius = 0,
        AnimalUnit targetUnit = null,
        bool overrideExisting = false
    )
    {
        if (objectives.ContainsKey(type) && !overrideExisting && (priority <= objectives[type].priority))
        {
            Debug.LogWarning($"Objective of type {type} already exists and overrideExisting is false. Objective not added.");
            return;
        }

        objectives[type] = new ObjectiveData(
            priority,
            turnsToExpiration,
            turnOfCreation,
            targetTile,
            targetAreaRadius,
            targetUnit
        );
    }

    /// <summary>
    /// Removes an objective from the blackboard. If the objective does not exist, a warning is logged.
    /// </summary>
    public void RemoveObjective(ObjectiveTypes type)
    {
        if (objectives.ContainsKey(type))
        {
            objectives.Remove(type);
        }
        else
        {
            Debug.LogWarning($"Objective of type {type} does not exist. Cannot remove.");
        }
    }

    public void RemoveExpiredObjectives(int currentTurn)
    {
        List<ObjectiveTypes> objectivesToRemove = new List<ObjectiveTypes>();

        foreach (var kvp in objectives)
        {
            ObjectiveData data = kvp.Value;
            if (currentTurn - data.turnOfCreation >= data.turnsToExpiration)
            {
                objectivesToRemove.Add(kvp.Key);
            }
        }

        foreach (var type in objectivesToRemove)
        {
            objectives.Remove(type);
        }
    }

    public void ClearObjectives()
    {
        objectives.Clear();
    }
    
    public ObjectiveData? GetObjective(ObjectiveTypes type)
    {
        if (objectives.TryGetValue(type, out ObjectiveData data))
        {
            return data;
        }
        return null;
    }
    public ObjectiveData? GetHighestPriorityObjective()
    {
        ObjectiveData? highestPriorityObjective = null;

        foreach (var kvp in objectives)
        {
            if (highestPriorityObjective == null || kvp.Value.priority > highestPriorityObjective.Value.priority)
            {
                highestPriorityObjective = kvp.Value;
            }
        }

        return highestPriorityObjective;
    }

    public Dictionary<ObjectiveTypes, ObjectiveData> GetAllObjectives()
    {
        return new Dictionary<ObjectiveTypes, ObjectiveData>(objectives);
    }

}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TriggerData
{
    public GameObject triggerObject;
    public List<PawnBrain> enemies = new List<PawnBrain>();
    public virtual bool IsEnemiesDestroyed => enemies.Find(enemy => enemy.GetSelectableType() == SelectableType.Enemy) == null;
    [SerializeField, HideInInspector]
    public bool isActive = false;
}

/// <summary>
/// Запуск боя: через коллайдер (если задан <see cref="TriggerData.triggerObject"/> на объекте с BoxCollider) или через <see cref="TurnManager.StartDelayedEncounter"/>.
/// Если коллайдер не нужен, оставьте triggerObject пустым и вызывайте только StartDelayedEncounter — что сработает раньше (вход в зону или вызов метода), то и запускает спавн и бой.
/// </summary>
[System.Serializable]
public class DelayedTriggerData : TriggerData
{
    [System.Serializable]
    public class SpawnableInstance
    {
        public GameObject enemy;
        public Transform where;
    }
    public List<SpawnableInstance> enemySpawnPoints = new List<SpawnableInstance>();
    [SerializeField, HideInInspector]
    public bool spawned = false;
}

public class TurnManager : MonoBehaviour
{
    [System.Serializable]
    public class TurnSlot
    {
        public IControlableSelectable pawn;
        public float initiative;
    }

    [System.Serializable]
    private class DynamicEnemyRegistryEntry
    {
        public int registryIndex;
        public int delayedTriggerIndex;
        public int spawnPointIndex;
        public Vector3 position;
        public Quaternion rotation;
        public string prefabName;
    }

    public static TurnManager Instance { get; private set; }
    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;

    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;

    public event Action OnTriggerZoneEnter;
    /// <summary>
    /// Вызывается при выходе из боя до сброса параметров пешек (ResetActionPoints).
    /// Нужен для формул прогресса, использующих LastRoundWalked — иначе WALKED уже скопирован в LastRoundWalked и формула даёт 0.
    /// </summary>
    public event Action OnTriggerZoneExitBeforePawnReset;
    public event Action OnTriggerZoneExit;
    public event Action OnTurnQueueChanged;
    public bool IsQuarantine = false;

    [SerializeField]
    private List<TriggerData> listOfTriggers = new List<TriggerData>();
    [SerializeField]
    private List<DelayedTriggerData> listOfDelayedTriggers = new List<DelayedTriggerData>();
    public bool IsPlayerTurn { get; private set; } = true;
    public IControlableSelectable CurrentActor { get; private set; }
    public IReadOnlyList<TurnSlot> RoundQueue => roundQueue;
    public int CurrentQueueIndex => currentQueueIndex;
    [SerializeField]
    private NavMeshSurface navMeshSurface;
    private List<UnityEngine.Object> movingPawns = new List<UnityEngine.Object>();
    [SerializeField]
    private IconButtonStyleFiller endTurnButton1;
    [Header("Initiative")]
    [SerializeField]
    private float defaultDexterity = 1f;
    private readonly List<TurnSlot> roundQueue = new List<TurnSlot>();
    private int currentQueueIndex = 0;
    private bool turnInProgress = false;
    private SelectableType lastActorSide = (SelectableType)(-1);
    private AsyncOperation navMeshUpdateOp;
    private const string UNIQUE_ID = "TurnManager";
    private const string DYNAMIC_ENEMY_NAME_PREFIX = "EnemySpawned";
    private readonly List<DynamicEnemyRegistryEntry> dynamicEnemyRegistry = new List<DynamicEnemyRegistryEntry>();
    private readonly List<GameObject> spawnedDynamicEnemies = new List<GameObject>();
    private int nextDynamicEnemyIndex = 0;
    private AudioSource stationWarnings;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // If we need to save current state across scenes, uncomment the next line
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        stationWarnings = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
        StartCoroutine(DelayFrame(() => SyncEndTurnButtonsWithMovement()));
    }
    private void OnLoadData(LoadedData data)
    {
        IsPlayerTurn = data.GetData("IsPlayerTurn", UNIQUE_ID, IsPlayerTurn);
        for (int i = 0; i < listOfTriggers.Count; i++)
        {
            listOfTriggers[i].isActive = data.GetData("IsTriggerActive_" + i, UNIQUE_ID, listOfTriggers[i].isActive);
        }
        for (int i = 0; i < listOfDelayedTriggers.Count; i++)
        {
            listOfDelayedTriggers[i].isActive = data.GetData("IsDelayedTriggerActive_" + i, UNIQUE_ID, listOfDelayedTriggers[i].isActive);
            listOfDelayedTriggers[i].spawned = data.GetData("IsDelayedTriggerSpawned_" + i, UNIQUE_ID, listOfDelayedTriggers[i].spawned);
        }
        IsQuarantine = data.GetData("IsQuarantine", UNIQUE_ID, false);
        RebuildDynamicEnemiesFromSave(data);
        bool inCombat = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f
            || listOfTriggers.Exists(t => t.isActive)
            || listOfDelayedTriggers.Exists(t => t.isActive);
        if (inCombat)
        {
            UpdateNavMesh();
            StartCoroutine(DelayFrame(() =>
            {
                RebuildRoundQueue();
                currentQueueIndex = 0;
                StartCurrentActorTurn();
            }));
        }
    }
    private void OnSaveData(Action<SaveRecord[], string> addSaveData)
    {
        SyncDynamicRegistryTransforms();
        List<SaveRecord> records = new List<SaveRecord>();
        records.Add(new SaveRecord
        {
            recordName = "IsPlayerTurn",
            recordType = SaveRecordType.boolean,
            boolValue = IsPlayerTurn
        });
        for (int i = 0; i < listOfTriggers.Count; i++)
        {
            records.Add(new SaveRecord
            {
                recordName = "IsTriggerActive_" + i,
                recordType = SaveRecordType.boolean,
                boolValue = listOfTriggers[i].isActive
            });
        }
        for (int i = 0; i < listOfDelayedTriggers.Count; i++)
        {
            records.Add(new SaveRecord
            {
                recordName = "IsDelayedTriggerActive_" + i,
                recordType = SaveRecordType.boolean,
                boolValue = listOfDelayedTriggers[i].isActive
            });
            records.Add(new SaveRecord
            {
                recordName = "IsDelayedTriggerSpawned_" + i,
                recordType = SaveRecordType.boolean,
                boolValue = listOfDelayedTriggers[i].spawned
            });
        }
        records.Add(new SaveRecord
        {
            recordName = "DynamicEnemyCount",
            recordType = SaveRecordType.integerNumber,
            intValue = dynamicEnemyRegistry.Count
        });
        for (int i = 0; i < dynamicEnemyRegistry.Count; i++)
        {
            DynamicEnemyRegistryEntry entry = dynamicEnemyRegistry[i];
            records.Add(new SaveRecord
            {
                recordName = "DynamicEnemyRegistryIndex_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = entry.registryIndex
            });
            records.Add(new SaveRecord
            {
                recordName = "DynamicEnemyDelayedTriggerIndex_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = entry.delayedTriggerIndex
            });
            records.Add(new SaveRecord
            {
                recordName = "DynamicEnemySpawnPointIndex_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = entry.spawnPointIndex
            });
            records.Add(new SaveRecord
            {
                recordName = "DynamicEnemyPos_" + i,
                recordType = SaveRecordType.vector,
                vecValue = entry.position
            });
            records.Add(new SaveRecord
            {
                recordName = "DynamicEnemyRot_" + i,
                recordType = SaveRecordType.quaternion,
                quatValue = entry.rotation
            });
            records.Add(new SaveRecord
            {
                recordName = "DynamicEnemyPrefabName_" + i,
                recordType = SaveRecordType.stringValue,
                stringValue = entry.prefabName ?? string.Empty
            });
        }
        records.Add(new SaveRecord
        {
            recordName = "IsQuarantine",
            recordType = SaveRecordType.boolean,
            boolValue = IsQuarantine
        });
        addSaveData(records.ToArray(), UNIQUE_ID);
    }
    public void EnterTrigger(GameObject triggerObject, IControlableSelectable enterer = null)
    {
        TriggerData trigger = listOfTriggers.Find(t => t.triggerObject == triggerObject);
        if (trigger == null)
        {
            trigger = listOfDelayedTriggers.Find(t => t.triggerObject == triggerObject);
            if (trigger == null)
            {
                return;
            }
            DelayedTriggerData delayed = trigger as DelayedTriggerData;
            if (!delayed.spawned)
            {
                if (!EnterDelayedTrigger(delayed))
                {
                    return;
                }
            }
        }
        IsQuarantine = false;
        ActivateCombatForTrigger(trigger, enterer);
    }

    /// <summary>
    /// Запускает бой по записи delayed-триггера без входа в зону коллайдера: спавн врагов (если ещё не были) и та же активация, что при EnterTrigger.
    /// </summary>
    public bool StartDelayedEncounter(DelayedTriggerData delayed)
    {
        if (delayed == null || !listOfDelayedTriggers.Contains(delayed))
        {
            return false;
        }
        if (!delayed.spawned && !EnterDelayedTrigger(delayed))
        {
            return false;
        }
        stationWarnings.Play();
        IsQuarantine = true;
        return ActivateCombatForTrigger(delayed, null);
    }

    /// <inheritdoc cref="StartDelayedEncounter(DelayedTriggerData)"/>
    public bool StartDelayedEncounterByIndex(int delayedTriggerIndex)
    {
        if (delayedTriggerIndex < 0 || delayedTriggerIndex >= listOfDelayedTriggers.Count)
        {
            return false;
        }
        return StartDelayedEncounter(listOfDelayedTriggers[delayedTriggerIndex]);
    }

    private bool ActivateCombatForTrigger(TriggerData trigger, IControlableSelectable enterer)
    {
        if (trigger == null)
        {
            return false;
        }
        if (trigger.IsEnemiesDestroyed)
        {
            return false;
        }
        if (trigger.isActive)
        {
            return false;
        }
        bool alreadyInCombat = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f;
        bool wasAnyActive = listOfTriggers.Find(t => t.isActive) != null
            || listOfDelayedTriggers.Find(t => t.isActive) != null;
        trigger.isActive = true;
        HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] = 1f;
        if (enterer != null)
            GroupMove.RallyForCombat(enterer, CollectEnemyPositions(trigger));
        if (!wasAnyActive)
        {
            OnTriggerZoneEnter?.Invoke();
            UILayersController.Instance.ShowOverlay(UILayersController.UILayer.AttentionText, "_notpersistent_0_GameAttentionColor");
        }
        SyncEndTurnButtonsWithMovement();
        if (alreadyInCombat)
        {
            AppendMissingCombatantsToQueue();
            OnTurnQueueChanged?.Invoke();
        }
        else
        {
            StartCoroutine(StartFirstTurn());
        }
        return true;
    }

    List<Vector3> CollectEnemyPositions(TriggerData trigger)
    {
        var list = new List<Vector3>();
        if (trigger == null || trigger.enemies == null) return list;
        for (int i = 0; i < trigger.enemies.Count; i++)
        {
            if (trigger.enemies[i] == null) continue;
            list.Add(trigger.enemies[i].transform.position);
        }
        return list;
    }
    public bool EnterDelayedTrigger(DelayedTriggerData trigger)
    {
        if (trigger == null)
        {
            return false;
        }
        if (trigger.spawned)
        {
            return false;
        }
        int delayedTriggerIndex = listOfDelayedTriggers.IndexOf(trigger);
        if (delayedTriggerIndex < 0)
        {
            return false;
        }
        SpawnAllFromDelayedTrigger(delayedTriggerIndex, registerInRegistry: true, loadData: null);
        trigger.spawned = true;
        return true;
    }
    private void RebuildDynamicEnemiesFromSave(LoadedData data)
    {
        ClearDynamicEnemiesRuntime();
        dynamicEnemyRegistry.Clear();
        int savedCount = data.GetData("DynamicEnemyCount", UNIQUE_ID, 0);
        for (int i = 0; i < savedCount; i++)
        {
            DynamicEnemyRegistryEntry entry = new DynamicEnemyRegistryEntry
            {
                registryIndex = data.GetData("DynamicEnemyRegistryIndex_" + i, UNIQUE_ID, i),
                delayedTriggerIndex = data.GetData("DynamicEnemyDelayedTriggerIndex_" + i, UNIQUE_ID, -1),
                spawnPointIndex = data.GetData("DynamicEnemySpawnPointIndex_" + i, UNIQUE_ID, -1),
                position = data.GetData("DynamicEnemyPos_" + i, UNIQUE_ID, Vector3.zero),
                rotation = data.GetData("DynamicEnemyRot_" + i, UNIQUE_ID, Quaternion.identity),
                prefabName = data.GetData("DynamicEnemyPrefabName_" + i, UNIQUE_ID, string.Empty)
            };
            dynamicEnemyRegistry.Add(entry);
        }
        dynamicEnemyRegistry.Sort((left, right) => left.registryIndex.CompareTo(right.registryIndex));
        LoadedData loadData = data ?? SaveHub.Instance.CurrentLoadedData;
        if (dynamicEnemyRegistry.Count > 0)
        {
            foreach (DynamicEnemyRegistryEntry entry in dynamicEnemyRegistry)
            {
                SpawnFromRegistryEntry(entry, loadData);
            }
        }
        else
        {
            SpawnDelayedTriggerFallbackForLegacySaves(loadData);
        }
        nextDynamicEnemyIndex = 0;
        for (int i = 0; i < dynamicEnemyRegistry.Count; i++)
        {
            if (dynamicEnemyRegistry[i].registryIndex >= nextDynamicEnemyIndex)
            {
                nextDynamicEnemyIndex = dynamicEnemyRegistry[i].registryIndex + 1;
            }
        }
    }
    private void SpawnDelayedTriggerFallbackForLegacySaves(LoadedData loadData)
    {
        for (int i = 0; i < listOfDelayedTriggers.Count; i++)
        {
            if (!listOfDelayedTriggers[i].spawned)
            {
                continue;
            }
            SpawnAllFromDelayedTrigger(i, registerInRegistry: true, loadData: loadData);
        }
    }
    private void SpawnAllFromDelayedTrigger(int delayedTriggerIndex, bool registerInRegistry, LoadedData loadData)
    {
        if (delayedTriggerIndex < 0 || delayedTriggerIndex >= listOfDelayedTriggers.Count)
        {
            return;
        }
        DelayedTriggerData trigger = listOfDelayedTriggers[delayedTriggerIndex];
        for (int spawnPointIndex = 0; spawnPointIndex < trigger.enemySpawnPoints.Count; spawnPointIndex++)
        {
            DelayedTriggerData.SpawnableInstance spawnPoint = trigger.enemySpawnPoints[spawnPointIndex];
            if (spawnPoint.enemy == null || spawnPoint.where == null)
            {
                continue;
            }
            DynamicEnemyRegistryEntry entry = new DynamicEnemyRegistryEntry
            {
                registryIndex = nextDynamicEnemyIndex++,
                delayedTriggerIndex = delayedTriggerIndex,
                spawnPointIndex = spawnPointIndex,
                position = spawnPoint.where.position,
                rotation = spawnPoint.where.rotation,
                prefabName = spawnPoint.enemy.name
            };
            if (registerInRegistry)
            {
                dynamicEnemyRegistry.Add(entry);
            }
            SpawnFromRegistryEntry(entry, loadData);
        }
    }
    private void SpawnFromRegistryEntry(DynamicEnemyRegistryEntry entry, LoadedData loadData)
    {
        if (entry.delayedTriggerIndex < 0 || entry.delayedTriggerIndex >= listOfDelayedTriggers.Count)
        {
            return;
        }
        DelayedTriggerData trigger = listOfDelayedTriggers[entry.delayedTriggerIndex];
        if (entry.spawnPointIndex < 0 || entry.spawnPointIndex >= trigger.enemySpawnPoints.Count)
        {
            return;
        }
        DelayedTriggerData.SpawnableInstance spawnPoint = trigger.enemySpawnPoints[entry.spawnPointIndex];
        if (spawnPoint.enemy == null)
        {
            return;
        }
        GameObject enemy = Instantiate(spawnPoint.enemy, entry.position, entry.rotation);
        enemy.name = DYNAMIC_ENEMY_NAME_PREFIX + entry.registryIndex;
        spawnedDynamicEnemies.Add(enemy);
        PawnBrain pawnBrain = enemy.GetComponent<PawnBrain>();
        if (pawnBrain != null)
        {
            trigger.enemies.Add(pawnBrain);
            SimpleEnemyAI.Instance.AddPawnToScenario(pawnBrain);
            if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f)
                RegisterCombatant(pawnBrain);
        }
        if (loadData != null)
        {
            enemy.BroadcastMessage("OnLoadData", loadData, SendMessageOptions.DontRequireReceiver);
        }
    }
    private void ClearDynamicEnemiesRuntime()
    {
        for (int i = 0; i < listOfDelayedTriggers.Count; i++)
        {
            listOfDelayedTriggers[i].enemies.RemoveAll(enemy =>
                enemy == null ||
                enemy.gameObject == null ||
                enemy.gameObject.name.StartsWith(DYNAMIC_ENEMY_NAME_PREFIX));
        }
        for (int i = 0; i < spawnedDynamicEnemies.Count; i++)
        {
            if (spawnedDynamicEnemies[i] != null)
            {
                Destroy(spawnedDynamicEnemies[i]);
            }
        }
        spawnedDynamicEnemies.Clear();
    }
    private void SyncDynamicRegistryTransforms()
    {
        dynamicEnemyRegistry.RemoveAll(entry => entry == null);
        dynamicEnemyRegistry.Sort((left, right) => left.registryIndex.CompareTo(right.registryIndex));
        spawnedDynamicEnemies.RemoveAll(enemy => enemy == null);
        dynamicEnemyRegistry.RemoveAll(entry =>
            spawnedDynamicEnemies.Find(go => go != null && go.name == DYNAMIC_ENEMY_NAME_PREFIX + entry.registryIndex) == null);
        for (int i = 0; i < dynamicEnemyRegistry.Count; i++)
        {
            DynamicEnemyRegistryEntry entry = dynamicEnemyRegistry[i];
            GameObject enemy = spawnedDynamicEnemies.Find(go => go != null && go.name == DYNAMIC_ENEMY_NAME_PREFIX + entry.registryIndex);
            if (enemy != null)
            {
                entry.position = enemy.transform.position;
                entry.rotation = enemy.transform.rotation;
            }
        }
    }
    private void ExitAllTriggers()
    {
        OnTriggerZoneExitBeforePawnReset?.Invoke();
        HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] = 0f;
        ClearTurnQueue();
        CurrentActor = null;
        turnInProgress = false;
        lastActorSide = (SelectableType)(-1);
        OnTriggerZoneExit?.Invoke();
        IsQuarantine = false;
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.AttentionText, "_notpersistent_3_GameCongratulationsColor");
        SyncEndTurnButtonsWithMovement();
    }
    private void CheckTrigger(TriggerData trigger)
    {
        if (trigger.isActive)
        {
            if (trigger.IsEnemiesDestroyed)
            {
                trigger.isActive = false;
            }
        }
    }
    public void CheckTriggers()
    {
        bool wasInCombat = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f;
        foreach (var trigger in listOfTriggers)
        {
            CheckTrigger(trigger);
        }
        foreach (var trigger in listOfDelayedTriggers)
        {
            CheckTrigger(trigger);
        }
        bool anyActive = listOfTriggers.Find(t => t.isActive) != null
            || listOfDelayedTriggers.Find(t => t.isActive) != null;
        if (wasInCombat && !anyActive)
        {
            ExitAllTriggers();
        }
    }
    private bool IsInActiveTriggerZone(TriggerData trigger, PawnBrain pawn)
    {
        if (trigger.isActive)
        {
            if (trigger.enemies.Contains(pawn))
            {
                return true;
            }
        }
        return false;
    }
    public bool IsInActiveTriggerZone(PawnBrain pawn)
    {
        foreach (var trigger in listOfTriggers) if (IsInActiveTriggerZone(trigger, pawn)) return true;
        foreach (var trigger in listOfDelayedTriggers) if (IsInActiveTriggerZone(trigger, pawn)) return true;
        return false;
    }
    public void RegisterMovingPawn(UnityEngine.Object pawn)
    {
        if (listOfTriggers.Find(t => t.isActive) == null && listOfDelayedTriggers.Find(t => t.isActive) == null)
        {
            return;
        }
        if (pawn == null || movingPawns.Contains(pawn)) return;
        if (pawn is GameObject go)
        {
            var brain = go.GetComponent<PawnBrain>();
            if (brain != null && GroupMove.IsRallying(brain)) return;
        }
        movingPawns.Add(pawn);
        endTurnButton1.TurnOffButton();
    }
    public void UnregisterMovingPawn(UnityEngine.Object pawn)
    {
        if (pawn == null) return;
        while (movingPawns.Remove(pawn)) { }
        SyncEndTurnButtonsWithMovement();
    }

    private void SyncEndTurnButtonsWithMovement()
    {
        bool hasActiveTrigger = listOfTriggers.Find(t => t.isActive) != null;
        bool hasActiveDelayedTrigger = listOfDelayedTriggers.Find(t => t.isActive) != null;
        bool currentMoving = IsCurrentActorMoving();
        if ((hasActiveTrigger || hasActiveDelayedTrigger) && IsPlayerTurn && !currentMoving)
        {
            endTurnButton1.TurnOffButton();
        }
        else
        {
            endTurnButton1.TurnOnButton();
        }
    }

    bool IsCurrentActorMoving()
    {
        if (CurrentActor == null) return movingPawns.Count > 0;
        UnityEngine.Object go = CurrentActor.GetTransform() != null ? CurrentActor.GetTransform().gameObject : null;
        if (go == null) return false;
        return movingPawns.Contains(go);
    }

    private IEnumerator StartFirstTurn()
    {
        yield return null;
        RebuildRoundQueue();
        currentQueueIndex = 0;
        StartCurrentActorTurn();
    }
    private IEnumerator DelayFrame(System.Action action)
    {
        yield return null;
        action?.Invoke();
    }

    public float RollInitiative(IControlableSelectable pawn)
    {
        return DiceExpr.Roll("d20") + GetDexterity(pawn);
    }

    private float GetDexterity(IControlableSelectable pawn)
    {
        if (pawn == null) return defaultDexterity;
        try
        {
            return pawn.GetDynamicParameterValue(PawnDataController.DEXTERITY_KEY);
        }
        catch
        {
            return defaultDexterity;
        }
    }

    private bool IsAliveCombatant(IControlableSelectable pawn)
    {
        if (pawn == null) return false;
        SelectableType type = pawn.GetSelectableType();
        if (type == SelectableType.Dead) return false;
        if (type == SelectableType.Enemy && !pawn.IsInActiveTriggerZone()) return false;
        return type == SelectableType.Player || type == SelectableType.Enemy;
    }

    public void RebuildRoundQueue()
    {
        ClearTurnQueue();
        PawnBrain[] brains = FindObjectsByType<PawnBrain>(FindObjectsSortMode.None);
        for (int i = 0; i < brains.Length; i++)
        {
            PawnBrain brain = brains[i];
            if (!IsAliveCombatant(brain)) continue;
            if (GroupMove.IsRallying(brain)) continue;
            roundQueue.Add(new TurnSlot
            {
                pawn = brain,
                initiative = RollInitiative(brain)
            });
        }
        roundQueue.Sort((a, b) => b.initiative.CompareTo(a.initiative));
        OnTurnQueueChanged?.Invoke();
    }

    private void AppendMissingCombatantsToQueue()
    {
        PawnBrain[] brains = FindObjectsByType<PawnBrain>(FindObjectsSortMode.None);
        for (int i = 0; i < brains.Length; i++)
        {
            RegisterCombatant(brains[i]);
        }
    }

    public void RegisterCombatant(IControlableSelectable pawn)
    {
        if (!IsAliveCombatant(pawn)) return;
        if (GroupMove.IsRallying(pawn)) return;
        for (int i = 0; i < roundQueue.Count; i++)
        {
            if (roundQueue[i].pawn == pawn) return;
        }
        roundQueue.Add(new TurnSlot
        {
            pawn = pawn,
            initiative = RollInitiative(pawn)
        });
        OnTurnQueueChanged?.Invoke();
    }

    public void RegisterCombatantAtEnd(IControlableSelectable pawn)
    {
        if (!IsAliveCombatant(pawn)) return;
        for (int i = 0; i < roundQueue.Count; i++)
        {
            if (roundQueue[i].pawn == pawn) return;
        }
        roundQueue.Add(new TurnSlot
        {
            pawn = pawn,
            initiative = -9999f
        });
        OnTurnQueueChanged?.Invoke();
    }

    private void ClearTurnQueue()
    {
        roundQueue.Clear();
        currentQueueIndex = 0;
        OnTurnQueueChanged?.Invoke();
    }

    private void PruneDeadFromQueue()
    {
        bool changed = false;
        for (int i = roundQueue.Count - 1; i >= 0; i--)
        {
            if (!IsAliveCombatant(roundQueue[i].pawn))
            {
                if (i < currentQueueIndex) currentQueueIndex--;
                roundQueue.RemoveAt(i);
                changed = true;
            }
        }
        if (currentQueueIndex < 0) currentQueueIndex = 0;
        if (changed) OnTurnQueueChanged?.Invoke();
    }

    private void StartCurrentActorTurn()
    {
        StartCurrentActorTurn(0);
    }

    private void StartCurrentActorTurn(int skipGuard)
    {
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f)
            return;
        if (skipGuard > 64)
        {
            Debug.LogError("TurnManager: StartCurrentActorTurn skip guard exceeded");
            return;
        }
        PruneDeadFromQueue();
        if (roundQueue.Count == 0)
        {
            CheckTriggers();
            return;
        }
        if (currentQueueIndex >= roundQueue.Count)
            currentQueueIndex = 0;
        CurrentActor = roundQueue[currentQueueIndex].pawn;
        if (!IsAliveCombatant(CurrentActor) || GroupMove.IsRallying(CurrentActor))
        {
            currentQueueIndex++;
            StartCurrentActorTurn(skipGuard + 1);
            return;
        }
        turnInProgress = true;
        IsPlayerTurn = CurrentActor.GetSelectableType() == SelectableType.Player;
        SelectableType side = CurrentActor.GetSelectableType();
        if (side != lastActorSide)
        {
            lastActorSide = side;
            UpdateNavMesh();
        }
        OnTurnQueueChanged?.Invoke();
        if (IsPlayerTurn)
            OnPlayerTurnStart?.Invoke();
        else
            OnEnemyTurnStart?.Invoke();
        SyncEndTurnButtonsWithMovement();
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn) return;
        EndCurrentActorTurn();
    }

    public void EndEnemyTurn()
    {
        if (IsPlayerTurn) return;
        EndCurrentActorTurn();
    }

    public void EndCurrentActorTurn()
    {
        if (!turnInProgress) return;
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f) return;
        if (IsCurrentActorMoving()) return;
        CheckTriggers();
        if (listOfTriggers.Find(t => t.isActive) == null && listOfDelayedTriggers.Find(t => t.isActive) == null)
            return;
        turnInProgress = false;
        if (IsPlayerTurn)
            OnPlayerTurnEnd?.Invoke();
        else
            OnEnemyTurnEnd?.Invoke();
        currentQueueIndex++;
        StartCoroutine(AdvanceToNextActor());
    }

    private IEnumerator AdvanceToNextActor()
    {
        yield return null;
        StartCurrentActorTurn();
    }

    public void UpdateNavMesh()
    {
        if (navMeshSurface == null || navMeshSurface.navMeshData == null) return;
        if (navMeshUpdateOp != null && !navMeshUpdateOp.isDone) return;
        navMeshUpdateOp = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }

    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
        }
    }
}

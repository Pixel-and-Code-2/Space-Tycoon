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
    public bool IsEnemiesDestroyed => enemies.Find(enemy => enemy.GetSelectableType() == SelectableType.Enemy) == null;
    [SerializeField, HideInInspector]
    public bool isActive = false;
}

public class TurnManager : MonoBehaviour
{
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

    [SerializeField]
    private List<TriggerData> listOfTriggers = new List<TriggerData>();
    public bool IsPlayerTurn { get; private set; } = true;
    [SerializeField]
    private NavMeshSurface navMeshSurface;
    private List<UnityEngine.Object> movingPawns = new List<UnityEngine.Object>();
    [SerializeField]
    private IconButtonStyleFiller endTurnButton1;
    [SerializeField]
    private IconButtonStyleFiller endTurnButton2;
    private const string UNIQUE_ID = "TurnManager";

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
        if (IsPlayerTurn)
        {
            UpdateNavMesh();
            StartCoroutine(DelayFrame(() => StartPlayerTurn()));
        }
        else
        {
            UpdateNavMesh();
            StartCoroutine(DelayFrame(() => StartEnemyTurn()));
        }
    }
    private void OnSaveData(Action<SaveRecord[], string> addSaveData)
    {
        SaveRecord[] records = new SaveRecord[1 + listOfTriggers.Count];
        records[0] = new()
        {
            recordName = "IsPlayerTurn",
            recordType = SaveRecordType.boolean,
            boolValue = IsPlayerTurn
        };
        for (int i = 0; i < listOfTriggers.Count; i++)
        {
            records[i + 1] = new()
            {
                recordName = "IsTriggerActive_" + i,
                recordType = SaveRecordType.boolean,
                boolValue = listOfTriggers[i].isActive
            };
        }
        addSaveData(records, UNIQUE_ID);
    }
    public void EnterTrigger(GameObject triggerObject)
    {
        TriggerData trigger = listOfTriggers.Find(t => t.triggerObject == triggerObject);
        if (trigger == null)
        {
            return;
        }
        if (trigger.IsEnemiesDestroyed)
        {
            return;
        }
        if (trigger.isActive)
        {
            return;
        }
        if (listOfTriggers.Find(t => t.isActive) == null)
        {
            OnTriggerZoneEnter?.Invoke();
            UILayersController.Instance.ShowOverlay(UILayersController.UILayer.AttentionText, "Вторжение!_notpersistent_0_GameAttentionColor");
        }
        trigger.isActive = true;
        HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] = 1f;
        SyncEndTurnButtonsWithMovement();
        StartFirstTurn();
    }
    private void ExitAllTriggers()
    {
        OnTriggerZoneExitBeforePawnReset?.Invoke();
        HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] = 0f;
        OnTriggerZoneExit?.Invoke();
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.AttentionText, "Устранено!_notpersistent_3_GameCongratulationsColor");
        SyncEndTurnButtonsWithMovement();
    }

    public void CheckTriggers()
    {
        foreach (var trigger in listOfTriggers)
        {
            if (trigger.isActive)
            {
                if (trigger.IsEnemiesDestroyed)
                {
                    trigger.isActive = false;
                }
            }
        }
        if (listOfTriggers.Find(t => t.isActive) == null)
        {
            ExitAllTriggers();
        }
    }
    public bool IsInActiveTriggerZone(PawnBrain pawn)
    {
        foreach (var trigger in listOfTriggers)
        {
            if (trigger.isActive)
            {
                if (trigger.enemies.Contains(pawn))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public void RegisterMovingPawn(UnityEngine.Object pawn)
    {
        if (listOfTriggers.Find(t => t.isActive) == null) return;
        movingPawns.Add(pawn);
        endTurnButton1.TurnOffButton();
        endTurnButton2.TurnOffButton();
    }
    public void UnregisterMovingPawn(UnityEngine.Object pawn)
    {
        movingPawns.Remove(pawn);
        SyncEndTurnButtonsWithMovement();
    }

    private void SyncEndTurnButtonsWithMovement()
    {
        bool hasActiveTrigger = listOfTriggers.Find(t => t.isActive) != null;
        if (hasActiveTrigger && IsPlayerTurn && movingPawns.Count == 0)
        {
            endTurnButton1.TurnOnButton();
            endTurnButton2.TurnOnButton();
        }
        else
        {
            endTurnButton1.TurnOffButton();
            endTurnButton2.TurnOffButton();
        }
    }

    private IEnumerator StartFirstTurn()
    {
        yield return null; // Wait one frame to ensure all components are initialized
        EndEnemyTurn();
    }
    private IEnumerator DelayFrame(System.Action action)
    {
        yield return null;
        action?.Invoke();
    }

    private void StartPlayerTurn()
    {
        IsPlayerTurn = true;
        OnPlayerTurnStart?.Invoke();
        SyncEndTurnButtonsWithMovement();
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn) return;
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f) return;
        if (movingPawns.Count > 0) return;
        CheckTriggers();
        if (listOfTriggers.Find(t => t.isActive) == null)
        {
            return;
        }
        IsPlayerTurn = false;
        OnPlayerTurnEnd?.Invoke();
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        UpdateNavMesh();
        EnemyTurn();
    }

    private void EnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();
    }

    public void EndEnemyTurn()
    {
        if (IsPlayerTurn) return;
        OnEnemyTurnEnd?.Invoke();
        StartCoroutine(StartPlayerTurnWithDelay());
    }

    private IEnumerator StartPlayerTurnWithDelay()
    {
        // yield return new WaitForSeconds(0.1f);
        UpdateNavMesh();
        // yield return new WaitForSeconds(0.1f);
        StartPlayerTurn();
        yield return null;
    }

    public void UpdateNavMesh()
    {
        navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class PawnDataController : MonoBehaviour
{
    [SerializeField]
    private CombatantStats combatantStats;
    [Header("Additional Developing params")]
    [SerializeField, Tooltip("Max distance from mouse to walkable area, to show path")]
    public float maxSampleDistance = 5f;
    [SerializeField, Tooltip("Force of push when pawn collides with obstacle, 0 to disable")]
    public float obstaclePushForce = 10f;
    [SerializeField, Tooltip("Override of vertical push when pawn collides with obstacle, -1 to disable")]
    public float verticalPushOverride = 0.2f;

    float maxHp;
    float movePerTurn;
    float strength;
    float dexterity;
    float armorClass;
    float attackRange;

    float currentHp;
    float availableDistance;
    float walkedDistance;
    float lastRoundWalked;
    float shotAmount;
    float lastRoundShot;
    float meleeAmount;
    float lastRoundMelee;
    float movesToSkip;
    bool hasMovedThisTurn;
    float healingsAmount;

    public const string AVAILABLE_DISTANCE_KEY = "AvailableDistance";
    public const string INITIAL_HP_KEY = "HP";
    public const string INITIAL_AVAILABLE_DISTANCE_KEY = "SPD";
    public const string AVAILABLE_HEALTH_KEY = "AvailableHealth";
    public const string LAST_ROUND_WALKED_KEY = "LastRoundWalked";
    public const string WALKED_KEY = "WalkedDistance";
    public const string LAST_ROUND_SHOOTED_AMOUNT_KEY = "LastRoundShotAmount";
    public const string SHOOTED_AMOUNT_KEY = "ShotAmount";
    public const string LAST_ROUND_MELEE_AMOUNT_KEY = "LastRoundMeleeAmount";
    public const string MELEE_AMOUNT_KEY = "MeleeAmount";
    public const string MOVES_TO_SKIP_KEY = "MovesToSkip";
    public const string IS_SHOOT_ON_MOVE_KEY = "IsShootOnMove";
    public const string AMOUNT_OF_HEALINGS_KEY = "HealingsAmount";
    public const string DEXTERITY_KEY = "DEX";

    [SerializeField]
    public SelectableType selectableType = SelectableType.Player;
    [Header("Initial state")]
    [SerializeField]
    private bool startDead = false;
    public bool StartDead => startDead;
    public string UNIQUE_ID => "PawnData_" + gameObject.name;

    public CombatantStats Stats => combatantStats;
    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public float MovePerTurn => movePerTurn;
    public float Strength => strength;
    public float Dexterity => dexterity;
    public float ArmorClass => armorClass;
    public float AttackRange => attackRange;
    public float AvailableDistance => availableDistance;
    public float WalkedDistance => walkedDistance;
    public bool HasMovedThisTurn => hasMovedThisTurn;
    public bool HasRanged => combatantStats != null && combatantStats.HasRanged;
    public float ShotAmount => shotAmount;
    public float MeleeAmount => meleeAmount;
    public float MovesToSkip => movesToSkip;
    public float HealingsAmount => healingsAmount;

    public float RollMeleeDamage() => combatantStats != null ? combatantStats.RollMeleeDamage() : 1f;
    public float RollRangedDamage() => combatantStats != null ? combatantStats.RollRangedDamage() : 1f;

    void OnValidate()
    {
        if (!Application.isPlaying && combatantStats != null)
            ApplyStatsFromAsset(false);
    }

    void Start()
    {
        ApplyStatsFromAsset(true);
        if (startDead)
        {
            selectableType = SelectableType.Dead;
            currentHp = 0f;
        }
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        }
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
        TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        TurnManager.Instance.OnTriggerZoneEnter += OnTriggerZoneEnter;
    }

    void ApplyStatsFromAsset(bool resetRuntime)
    {
        if (combatantStats == null) return;
        maxHp = combatantStats.RollMaxHp();
        movePerTurn = combatantStats.RollMove();
        strength = combatantStats.RollStrength();
        dexterity = combatantStats.RollDexterity();
        armorClass = combatantStats.RollArmorClass();
        attackRange = combatantStats.RollAttackRange();
        if (resetRuntime)
            ResetRuntimeFromStats();
    }

    void ResetRuntimeFromStats()
    {
        currentHp = maxHp;
        availableDistance = movePerTurn;
        walkedDistance = 0f;
        lastRoundWalked = 0f;
        shotAmount = 0f;
        lastRoundShot = 0f;
        meleeAmount = 0f;
        lastRoundMelee = 0f;
        movesToSkip = 0f;
        hasMovedThisTurn = false;
        if (selectableType == SelectableType.Enemy
            && HandleInittingGlobalVars.globalParameters != null
            && HandleInittingGlobalVars.globalParameters.parametersDict.ContainsKey(HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY))
            healingsAmount = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
        else
            healingsAmount = 0f;
    }

    private void OnTriggerZoneExit()
    {
        ResetActionPoints();
        movesToSkip = 0f;
    }
    private void OnTriggerZoneEnter()
    {
    }

    void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        }
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
        }
    }

    private void OnPlayerTurnStart()
    {
        if (selectableType != SelectableType.Player) return;
        if (!IsCurrentActor()) return;
        ResetActionPoints();
    }

    private void OnEnemyTurnStart()
    {
        if (selectableType != SelectableType.Enemy) return;
        if (!IsCurrentActor()) return;
        ResetActionPoints();
    }

    private bool IsCurrentActor()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentActor == null) return true;
        PawnBrain brain = GetComponent<PawnBrain>();
        return brain != null && TurnManager.Instance.CurrentActor == brain;
    }

    private void OnLoadData(LoadedData data)
    {
        currentHp = data.GetData("CurrentHp", UNIQUE_ID, currentHp);
        availableDistance = data.GetData("AvailableDistance", UNIQUE_ID, availableDistance);
        walkedDistance = data.GetData("WalkedDistance", UNIQUE_ID, walkedDistance);
        hasMovedThisTurn = data.GetData("HasMovedThisTurn", UNIQUE_ID, hasMovedThisTurn ? 1f : 0f) > 0.5f;
        healingsAmount = data.GetData("HealingsAmount", UNIQUE_ID, healingsAmount);
        string selectableTypeKey = DataCompressor.GetRecordName("SelectableType", UNIQUE_ID);
        bool hasSelectableTypeInSave = data.intData != null && data.intData.ContainsKey(selectableTypeKey);
        selectableType = (SelectableType)data.GetData("SelectableType", UNIQUE_ID, (int)selectableType);
        if (startDead && !hasSelectableTypeInSave)
        {
            selectableType = SelectableType.Dead;
            currentHp = 0f;
        }
    }

    private void OnSaveData(System.Action<SaveRecord[], string> addSaveData)
    {
        addSaveData(new SaveRecord[] {
            new SaveRecord() { recordName = "CurrentHp", recordType = SaveRecordType.floatNumber, floatValue = currentHp },
            new SaveRecord() { recordName = "AvailableDistance", recordType = SaveRecordType.floatNumber, floatValue = availableDistance },
            new SaveRecord() { recordName = "WalkedDistance", recordType = SaveRecordType.floatNumber, floatValue = walkedDistance },
            new SaveRecord() { recordName = "HasMovedThisTurn", recordType = SaveRecordType.floatNumber, floatValue = hasMovedThisTurn ? 1f : 0f },
            new SaveRecord() { recordName = "HealingsAmount", recordType = SaveRecordType.floatNumber, floatValue = healingsAmount },
            new SaveRecord() { recordName = "SelectableType", recordType = SaveRecordType.integerNumber, intValue = (int)selectableType }
        }, UNIQUE_ID);
    }

    public float GetParameterValue(string parameterName)
    {
        switch (parameterName)
        {
            case INITIAL_HP_KEY: return maxHp;
            case AVAILABLE_HEALTH_KEY: return currentHp;
            case INITIAL_AVAILABLE_DISTANCE_KEY: return movePerTurn;
            case AVAILABLE_DISTANCE_KEY: return availableDistance;
            case WALKED_KEY: return walkedDistance;
            case LAST_ROUND_WALKED_KEY: return lastRoundWalked;
            case SHOOTED_AMOUNT_KEY: return shotAmount;
            case LAST_ROUND_SHOOTED_AMOUNT_KEY: return lastRoundShot;
            case MELEE_AMOUNT_KEY: return meleeAmount;
            case LAST_ROUND_MELEE_AMOUNT_KEY: return lastRoundMelee;
            case MOVES_TO_SKIP_KEY: return movesToSkip;
            case IS_SHOOT_ON_MOVE_KEY: return hasMovedThisTurn ? 1f : 0f;
            case AMOUNT_OF_HEALINGS_KEY: return healingsAmount;
            case DEXTERITY_KEY: return dexterity;
            default: return 0f;
        }
    }

    public void SetParameterValue(string parameterName, float value)
    {
        switch (parameterName)
        {
            case AVAILABLE_HEALTH_KEY: currentHp = value; break;
            case AVAILABLE_DISTANCE_KEY: availableDistance = value; break;
            case WALKED_KEY: walkedDistance = value; break;
            case LAST_ROUND_WALKED_KEY: lastRoundWalked = value; break;
            case SHOOTED_AMOUNT_KEY: shotAmount = value; break;
            case LAST_ROUND_SHOOTED_AMOUNT_KEY: lastRoundShot = value; break;
            case MELEE_AMOUNT_KEY: meleeAmount = value; break;
            case LAST_ROUND_MELEE_AMOUNT_KEY: lastRoundMelee = value; break;
            case MOVES_TO_SKIP_KEY: movesToSkip = value; break;
            case IS_SHOOT_ON_MOVE_KEY: hasMovedThisTurn = value > 0.5f; break;
            case AMOUNT_OF_HEALINGS_KEY: healingsAmount = value; break;
        }
        if (GameUI.Instance != null) GameUI.Instance.OnChangeStats();
        if (parameterName == AVAILABLE_DISTANCE_KEY || parameterName == AVAILABLE_HEALTH_KEY || parameterName == AMOUNT_OF_HEALINGS_KEY)
        {
            if (GameUI.Instance != null) GameUI.Instance.UpdatePlayerData();
        }
    }

    public void SetHasMovedThisTurn(bool value) => hasMovedThisTurn = value;

    public void ResetActionPoints()
    {
        availableDistance = movePerTurn;
        lastRoundWalked = walkedDistance;
        walkedDistance = 0f;
        lastRoundShot = shotAmount;
        shotAmount = 0f;
        lastRoundMelee = meleeAmount;
        meleeAmount = 0f;
        if (movesToSkip > 0) movesToSkip -= 1f;
        hasMovedThisTurn = false;
    }

    public static float CalculateLineStringDistance(Vector3[] points)
    {
        if (points == null || points.Length == 0) return 0f;
        float distance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
            distance += Vector3.Distance(points[i], points[i + 1]);
        return distance;
    }

    public void IsStepByStepOff()
    {
        movesToSkip = 0f;
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

using System;
using UnityEngine;

public class PartyProgress : MonoBehaviour
{
    public static PartyProgress Instance { get; private set; }

    public const string UNIQUE_ID = "PartyProgress";

    static readonly int[] XpToReachLevel =
    {
        0,
        0,
        600,
        1200,
        1850,
        2550,
        3300,
        4050,
        4850,
        5700,
        6600
    };

    static readonly int[] PointsOnReachLevel =
    {
        0,
        0,
        2,
        2,
        2,
        3,
        2,
        2,
        3,
        2,
        2
    };

    public const int XpKillMelee = 50;
    public const int XpKillShooter = 100;
    public const int XpKillTank = 300;
    public const int XpMainTask = 200;
    public const int XpSideTask = 100;

    [SerializeField]
    int totalXp;
    [SerializeField]
    int level = 1;

    public int TotalXp => totalXp;
    public int Level => level;
    public int MaxLevel => 10;

    public event Action OnProgressChanged;
    public event Action<int> OnXpGained;
    public event Action OnLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Second PartyProgress");
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnSave += OnSave;
            SaveHub.Instance.OnLoad += OnLoad;
        }
        RefreshFromXp(silent: true);
        OnProgressChanged?.Invoke();
    }

    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnSave -= OnSave;
            SaveHub.Instance.OnLoad -= OnLoad;
        }
        if (Instance == this) Instance = null;
    }

    public int XpForCurrentLevel()
    {
        int lv = Mathf.Clamp(level, 1, MaxLevel);
        return XpToReachLevel[lv];
    }

    public int XpForNextLevel()
    {
        if (level >= MaxLevel) return XpToReachLevel[MaxLevel];
        return XpToReachLevel[level + 1];
    }

    public float LevelProgress01()
    {
        if (level >= MaxLevel) return 1f;
        int cur = XpForCurrentLevel();
        int next = XpForNextLevel();
        if (next <= cur) return 1f;
        return Mathf.Clamp01((totalXp - cur) / (float)(next - cur));
    }

    public bool HasPendingLevelUpNotify { get; private set; }

    public void ClearLevelUpNotify()
    {
        HasPendingLevelUpNotify = false;
        OnProgressChanged?.Invoke();
    }

    public void NotifyProgressChanged()
    {
        OnProgressChanged?.Invoke();
    }

    public bool AnyUnspentSkillPoints()
    {
        if (GameUI.Instance != null)
            return GameUI.Instance.AnyPartyUnspentSkillPoints();
        foreach (var pawn in PawnBrain.AlivePlayers)
        {
            if (pawn == null) continue;
            PawnDataController data = pawn.GetComponent<PawnDataController>();
            if (data != null && data.UnspentSkillPoints > 0) return true;
        }
        return false;
    }

    public void GrantXp(int amount, IControlableSelectable source = null)
    {
        if (amount <= 0) return;
        totalXp += amount;
        ProgressionMessages.ShowXp(amount, source);
        int before = level;
        RefreshFromXp(silent: false);
        OnXpGained?.Invoke(amount);
        if (level > before)
        {
            HasPendingLevelUpNotify = true;
            OnLevelUp?.Invoke();
        }
        OnProgressChanged?.Invoke();
    }

    void RefreshFromXp(bool silent)
    {
        int newLevel = 1;
        for (int lv = MaxLevel; lv >= 2; lv--)
        {
            if (totalXp >= XpToReachLevel[lv])
            {
                newLevel = lv;
                break;
            }
        }
        if (newLevel == level) return;
        if (newLevel > level)
        {
            int gain = 0;
            for (int lv = level + 1; lv <= newLevel; lv++)
                gain += PointsOnReachLevel[lv];
            if (gain > 0)
                GrantPointsToEachCharacter(gain);
        }
        level = newLevel;
    }

    void GrantPointsToEachCharacter(int points)
    {
        if (GameUI.Instance != null)
        {
            GameUI.Instance.ForEachPartyPawn(d => d.AddSkillPoints(points));
            return;
        }
        foreach (var pawn in PawnBrain.AlivePlayers)
        {
            if (pawn == null) continue;
            PawnDataController data = pawn.GetComponent<PawnDataController>();
            if (data == null) continue;
            data.AddSkillPoints(points);
        }
    }

    public static int XpForEnemy(PawnDataController enemy)
    {
        if (enemy == null) return XpKillMelee;
        EnemyAiRole role = EnemyAiDecide.InferRole(enemy, enemy.AiProfileOverride);
        if (role == EnemyAiRole.Tank) return XpKillTank;
        if (role == EnemyAiRole.Shooter) return XpKillShooter;
        string n = enemy.Stats != null ? enemy.Stats.name : "";
        if (n.IndexOf("Tank", StringComparison.OrdinalIgnoreCase) >= 0) return XpKillTank;
        if (n.IndexOf("Shooter", StringComparison.OrdinalIgnoreCase) >= 0) return XpKillShooter;
        return XpKillMelee;
    }

    public static void GrantKillXp(IControlableSelectable killer, IAttackableSelectable victim)
    {
        if (Instance == null || killer == null || victim == null) return;
        PawnDataController kd = killer.GetComponent<PawnDataController>();
        if (kd == null || kd.selectableType != SelectableType.Player) return;
        PawnDataController vd = victim.GetComponent<PawnDataController>();
        Instance.GrantXp(XpForEnemy(vd), killer);
    }

    public static void GrantTaskXp(bool isMainTask, IControlableSelectable executor = null)
    {
        if (Instance == null) return;
        Instance.GrantXp(isMainTask ? XpMainTask : XpSideTask, executor);
    }

    void OnSave(Action<SaveRecord[], string> add)
    {
        add(new SaveRecord[]
        {
            new SaveRecord { recordName = "TotalXp", recordType = SaveRecordType.integerNumber, intValue = totalXp },
            new SaveRecord { recordName = "Level", recordType = SaveRecordType.integerNumber, intValue = level },
            new SaveRecord { recordName = "LevelUpNotify", recordType = SaveRecordType.boolean, boolValue = HasPendingLevelUpNotify }
        }, UNIQUE_ID);
    }

    void OnLoad(LoadedData data)
    {
        totalXp = data.GetData("TotalXp", UNIQUE_ID, 0);
        level = data.GetData("Level", UNIQUE_ID, 1);
        HasPendingLevelUpNotify = data.GetData("LevelUpNotify", UNIQUE_ID, false);
        if (level < 1) level = 1;
        if (level > MaxLevel) level = MaxLevel;
        // Legacy: party-wide SkillPoints → give each player that amount once if pawns have 0.
        int legacy = data.GetData("SkillPoints", UNIQUE_ID, 0);
        if (legacy > 0)
            MigrateLegacyPartyPoints(legacy);
        OnProgressChanged?.Invoke();
    }

    void MigrateLegacyPartyPoints(int legacy)
    {
        bool someoneHas = false;
        if (GameUI.Instance != null)
            GameUI.Instance.ForEachPartyPawn(d => { if (d.UnspentSkillPoints > 0) someoneHas = true; });
        if (someoneHas) return;
        GrantPointsToEachCharacter(legacy);
    }
}

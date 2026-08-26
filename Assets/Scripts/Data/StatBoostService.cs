using UnityEngine;
using System.Collections.Generic;

public static class StatBoostService
{
    public static bool IsSuccessColor(Color c)
    {
        return c.g >= 0.75f && c.r <= 0.35f && c.b <= 0.35f;
    }

    public static bool TryRoll(List<GlobalSettingsAssets.BoostEntry> pool, out GlobalSettingsAssets.BoostEntry entry)
    {
        entry = default;
        if (pool == null || pool.Count == 0) return false;
        entry = pool[Random.Range(0, pool.Count)];
        return true;
    }

    public static string FormatMessage(GlobalSettingsAssets.BoostEntry entry)
    {
        string stat = StatLabel(entry.stat);
        if (entry.mode == GlobalSettingsAssets.BoostMode.Percent)
            return "+ " + entry.value.ToString("0.#") + "% к " + stat;
        return "+ " + entry.value.ToString("0.#") + " к " + stat;
    }

    static string StatLabel(GlobalSettingsAssets.BoostStat stat)
    {
        switch (stat)
        {
            case GlobalSettingsAssets.BoostStat.Strength: return "силе";
            case GlobalSettingsAssets.BoostStat.Dexterity: return "ловкости";
            case GlobalSettingsAssets.BoostStat.ArmorClass: return "защите";
            case GlobalSettingsAssets.BoostStat.MaxHp: return "HP";
            default: return "стату";
        }
    }

    public static void ApplyToPawn(PawnDataController data, GlobalSettingsAssets.BoostEntry entry, Vector3 messagePos)
    {
        if (data == null) return;
        data.ApplyBoost(entry.stat, entry.mode, entry.value);
        if (UI3DManager.Instance != null)
            UI3DManager.Instance.ShowMessage(FormatMessage(entry), messagePos, new Color(0f, 1f, 0f));
    }

    public static void TryGrantAfterKill(IControlableSelectable killer)
    {
        if (killer == null) return;
        var pool = GlobalSettingsAssets.GetBoostPools().afterKill;
        if (!TryRoll(pool, out var entry)) return;
        var data = killer.GetComponent<PawnDataController>();
        if (data == null || data.selectableType != SelectableType.Player) return;
        ApplyToPawn(data, entry, killer.GetTransform().position);
    }

    public static void TryGrantAfterCombat()
    {
        var pool = GlobalSettingsAssets.GetBoostPools().afterCombat;
        if (pool == null || pool.Count == 0) return;
        foreach (var pawn in PawnBrain.AlivePlayers)
        {
            if (pawn == null) continue;
            if (!TryRoll(pool, out var entry)) continue;
            var data = pawn.GetComponent<PawnDataController>();
            if (data == null) continue;
            ApplyToPawn(data, entry, pawn.GetTransform().position);
        }
    }

    public static void TryGrantAfterTask(IControlableSelectable executor, ClickableItemsController.TaskItem completed)
    {
        if (executor == null || completed == null) return;
        if (string.IsNullOrEmpty(completed.completeText)) return;
        if (!IsSuccessColor(completed.completeTextColor)) return;
        var pool = GlobalSettingsAssets.GetBoostPools().afterTask;
        if (!TryRoll(pool, out var entry)) return;
        var data = executor.GetComponent<PawnDataController>();
        if (data == null) return;
        ApplyToPawn(data, entry, executor.GetTransform().position);
    }
}

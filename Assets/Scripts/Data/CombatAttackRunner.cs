using UnityEngine;
using System.Collections;

public class CombatAttackRunner : MonoBehaviour
{
    public static CombatAttackRunner Instance { get; private set; }

    bool busy;
    public bool IsBusy => busy || PhysicalDiceRoller.IsBusy;

    public void ForceClearBusy(string reason)
    {
        Debug.LogWarning("[DiceGate] CombatAttackRunner.ForceClearBusy: " + reason);
        busy = false;
        if (PhysicalDiceRoller.Instance != null && PhysicalDiceRoller.IsBusy)
            PhysicalDiceRoller.Instance.ForceUnlock(reason);
    }

    void Awake()
    {
        Instance = this;
    }

    public static CombatAttackRunner Ensure()
    {
        if (Instance != null) return Instance;
        GameObject host = PawnController.Instance != null ? PawnController.Instance.gameObject : null;
        if (host == null)
        {
            host = new GameObject("CombatAttackRunner");
            return host.AddComponent<CombatAttackRunner>();
        }
        return host.AddComponent<CombatAttackRunner>();
    }

    public bool ApplyResolved(
        IControlableSelectable attacker,
        IAttackableSelectable target,
        CombatResolver.Result r,
        Vector3 worldPoint,
        System.Action finished = null)
    {
        if (busy || PhysicalDiceRoller.IsBusy || attacker == null || target == null) return false;

        if (!r.hit)
        {
            Debug.Log("[DiceGate] ApplyResolved -> MISS (no damage die by design)");
            if (UI3DManager.Instance != null)
                UI3DManager.Instance.ShowMessage("Промах", worldPoint, Color.yellow);
            if (r.isMelee) attacker.OnMelee(worldPoint);
            else attacker.OnShoot(worldPoint, true);
            finished?.Invoke();
            return true;
        }

        if (r.crit && UI3DManager.Instance != null)
            UI3DManager.Instance.ShowMessage("Крит!", worldPoint, Color.magenta);

        if (r.waitPhysicalDice)
        {
            Debug.Log("[DiceGate] ApplyResolved -> wait dice path");
            busy = true;
            StartCoroutine(ApplyHitAfterDice(attacker, target, r, worldPoint, finished));
            return true;
        }

        Debug.Log(
            "[DiceGate] ApplyResolved -> SYNC hit (no waitPhysicalDice) dmg=" + r.damage
            + " runnerBusy=" + busy
            + " dieBusy=" + PhysicalDiceRoller.IsBusy);
        ApplyHit(attacker, target, r.damage, r.isMelee, worldPoint);
        finished?.Invoke();
        return true;
    }

    public bool ResolveAndApply(
        IControlableSelectable attacker,
        IAttackableSelectable target,
        Vector3 worldPoint,
        System.Action finished = null,
        bool forceDisadvantage = false)
    {
        if (busy || PhysicalDiceRoller.IsBusy || attacker == null || target == null) return false;
        PawnDataController atk = attacker.GetComponent<PawnDataController>();
        PawnDataController tgt = target.GetComponent<PawnDataController>();
        if (atk == null || tgt == null) return false;

        CombatResolver.Result r = CombatResolver.Resolve(
            atk, tgt,
            attacker.GetTransform().position,
            target.GetTransform().position,
            forceDisadvantage);

        if (!r.canAttack)
        {
            if (!string.IsNullOrEmpty(r.blockMessage) && UI3DManager.Instance != null)
                UI3DManager.Instance.ShowMessage(r.blockMessage, worldPoint, Color.red);
            finished?.Invoke();
            return true;
        }

        return ApplyResolved(attacker, target, r, worldPoint, finished);
    }

    IEnumerator ApplyHitAfterDice(
        IControlableSelectable attacker,
        IAttackableSelectable target,
        CombatResolver.Result r,
        Vector3 worldPoint,
        System.Action finished)
    {
        try
        {
            PawnDataController atk = attacker.GetComponent<PawnDataController>();
            string expr = DamageExpr(atk, r.isMelee);
            float dmg = 0f;
            bool done = false;
            bool fromCam = atk != null && atk.selectableType == SelectableType.Player;
            PhysicalDiceRoller roller = GetRoller();
            bool rollerEnabled = roller != null && roller.IsEnabled();
            Debug.Log(
                "[DiceGate] ApplyHitAfterDice expr=" + expr
                + " fromCam=" + fromCam
                + " roller=" + (roller != null)
                + " enabled=" + rollerEnabled
                + " dieBusy=" + PhysicalDiceRoller.IsBusy);
            if (rollerEnabled)
            {
                roller.RollDamage(expr, attacker.GetTransform().position, v =>
                {
                    dmg = v;
                    done = true;
                    Debug.Log("[DiceGate] physical roll done dmg=" + v);
                }, throwFromCamera: fromCam);
                float wait = 0f;
                while (!done)
                {
                    wait += Time.unscaledDeltaTime;
                    if (wait > 20f)
                    {
                        Debug.LogWarning("[DiceGate] ApplyHitAfterDice timeout — force unlock");
                        roller.ForceUnlock("ApplyHitAfterDice timeout");
                        if (!done) dmg = DiceExpr.Roll(expr);
                        done = true;
                    }
                    yield return null;
                }
            }
            else
            {
                dmg = DiceExpr.Roll(expr);
                Debug.Log("[DiceGate] FALLBACK sync DiceExpr.Roll dmg=" + dmg + " (wanted physical but roller disabled)");
            }

            if (r.crit) dmg *= 2f;
            ApplyHit(attacker, target, dmg, r.isMelee, worldPoint);
            finished?.Invoke();
        }
        finally
        {
            busy = false;
        }
    }

    static void ApplyHit(
        IControlableSelectable attacker,
        IAttackableSelectable target,
        float damage,
        bool isMelee,
        Vector3 worldPoint)
    {
        bool isAlive = target.OnGetHit(damage);
        if (isMelee)
            attacker.OnMelee(worldPoint);
        else
            attacker.OnShoot(worldPoint, isAlive);
        if (!isAlive)
            PartyProgress.GrantKillXp(attacker, target);
    }

    public static bool NeedsPhysicalDice()
    {
        var s = HandleInittingGlobalVars.globalSettingsAssets;
        PhysicalDiceRoller roller = GetRoller();
        bool settingsOk = s != null;
        bool flag = settingsOk && s.usePhysicalDice;
        bool rollerOk = roller != null;
        bool enabled = rollerOk && roller.IsEnabled();
        bool result = flag && enabled;
        Debug.Log(
            "[DiceGate] NeedsPhysicalDice=" + result
            + " settings=" + settingsOk
            + " usePhysicalDice=" + (settingsOk && s.usePhysicalDice)
            + " roller=" + rollerOk
            + " rollerEnabled=" + enabled
            + " dieBusy=" + PhysicalDiceRoller.IsBusy
            + " prefs=" + PlayerPrefs.GetInt("UsePhysicalDice", -1));
        return result;
    }

    static PhysicalDiceRoller GetRoller()
    {
        if (PhysicalDiceRoller.Instance != null) return PhysicalDiceRoller.Instance;
        return Object.FindFirstObjectByType<PhysicalDiceRoller>(FindObjectsInactive.Include);
    }

    static string DamageExpr(PawnDataController atk, bool isMelee)
    {
        if (atk == null || atk.Stats == null) return "1d6";
        if (isMelee) return string.IsNullOrWhiteSpace(atk.Stats.meleeDamage) ? "1d6" : atk.Stats.meleeDamage;
        if (!string.IsNullOrWhiteSpace(atk.Stats.rangedDamage)) return atk.Stats.rangedDamage;
        return string.IsNullOrWhiteSpace(atk.Stats.meleeDamage) ? "1d6" : atk.Stats.meleeDamage;
    }
}

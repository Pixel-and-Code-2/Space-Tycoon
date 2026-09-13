using UnityEngine;

public static class CombatResolver
{
    public struct Preview
    {
        public bool canAttack;
        public bool isMelee;
        public bool disadvantage;
        public float hitChance;
        public float distance;
        public string blockMessage;
    }

    public struct Result
    {
        public bool canAttack;
        public bool isMelee;
        public bool hit;
        public bool crit;
        public float damage;
        public float hitChance;
        public string blockMessage;
        public bool waitPhysicalDice;
    }

    public static Preview GetPreview(PawnDataController attacker, PawnDataController target, Vector3 attackerPos, Vector3 targetPos)
    {
        Preview p = new Preview();
        if (attacker == null || target == null)
        {
            p.blockMessage = "Нет цели";
            return p;
        }
        p.distance = Vector3.Distance(attackerPos, targetPos);
        float range = attacker.AttackRange;
        float reach = attacker.MeleeReach;
        p.isMelee = !attacker.HasRanged || p.distance <= reach;
        if (p.isMelee)
        {
            if (p.distance > reach + 0.05f)
            {
                p.blockMessage = "Слишком далеко";
                return p;
            }
            p.canAttack = true;
            p.disadvantage = attacker.HasMovedThisTurn;
        }
        else
        {
            if (HasWallBetween(attackerPos, targetPos))
            {
                p.blockMessage = "Стена";
                return p;
            }
            p.canAttack = true;
            bool rangeDisadv = p.distance >= range - 0.05f;
            p.disadvantage = attacker.HasMovedThisTurn || rangeDisadv;
        }
        float cost = attacker.GetAttackStaminaCost(p.isMelee);
        if (attacker.Stamina < cost - 0.001f)
        {
            p.canAttack = false;
            p.blockMessage = "Нет стамины";
            return p;
        }
        int mod = p.isMelee ? Mathf.RoundToInt(attacker.Strength) : Mathf.RoundToInt(attacker.Dexterity);
        int ac = Mathf.RoundToInt(target.ArmorClass);
        p.hitChance = HitChance(mod, ac, p.disadvantage);
        return p;
    }

    public static Result Resolve(
        PawnDataController attacker,
        PawnDataController target,
        Vector3 attackerPos,
        Vector3 targetPos,
        bool forceDisadvantage = false)
    {
        Preview p = GetPreview(attacker, target, attackerPos, targetPos);
        if (forceDisadvantage)
            p.disadvantage = true;
        if (!p.canAttack)
        {
            Result blocked = new Result();
            blocked.canAttack = false;
            blocked.isMelee = p.isMelee;
            blocked.hitChance = p.hitChance;
            blocked.blockMessage = p.blockMessage;
            return blocked;
        }
        int roll = RollAttackDie(p.disadvantage, out bool naturalCrit, out bool naturalFail);
        bool wantDice = CombatAttackRunner.NeedsPhysicalDice();
        Result r = FinalizeAttack(attacker, target, p, roll, naturalCrit, naturalFail, rollDamage: !wantDice);
        r.waitPhysicalDice = wantDice && r.hit;
        Debug.Log(
            "[DiceGate] Resolve hit=" + r.hit
            + " crit=" + r.crit
            + " wantDice=" + wantDice
            + " waitPhysicalDice=" + r.waitPhysicalDice
            + " rollDmgNow=" + !wantDice
            + " dmg=" + r.damage
            + " roll=" + roll
            + " melee=" + r.isMelee
            + " atk=" + (attacker != null ? attacker.name : "null"));
        return r;
    }

    public static Result FinalizeAttack(
        PawnDataController attacker,
        PawnDataController target,
        Preview p,
        int roll,
        bool naturalCrit,
        bool naturalFail,
        bool rollDamage = true)
    {
        Result r = new Result();
        r.canAttack = p.canAttack;
        r.isMelee = p.isMelee;
        r.hitChance = p.hitChance;
        r.blockMessage = p.blockMessage;
        if (!r.canAttack || attacker == null || target == null) return r;

        attacker.SpendStamina(attacker.GetAttackStaminaCost(p.isMelee));

        int mod = p.isMelee ? Mathf.RoundToInt(attacker.Strength) : Mathf.RoundToInt(attacker.Dexterity);
        int ac = Mathf.RoundToInt(target.ArmorClass);
        if (naturalFail)
        {
            r.hit = false;
            return r;
        }
        r.crit = naturalCrit;
        r.hit = r.crit || roll + mod >= ac;
        if (!r.hit) return r;
        if (rollDamage)
        {
            float dmg = p.isMelee ? attacker.RollMeleeDamage() : attacker.RollRangedDamage();
            if (r.crit) dmg *= 2f;
            r.damage = dmg;
        }
        return r;
    }

    public static int RollAttackDie(bool disadvantage, out bool naturalCrit, out bool naturalFail)
    {
        int a = RollOneAttackDie();
        int b = disadvantage ? RollOneAttackDie() : a;
        int roll = disadvantage ? Mathf.Min(a, b) : a;
        naturalFail = roll <= 1;
        naturalCrit = roll >= 20;
        return roll;
    }

    static int RollOneAttackDie()
    {
        var settings = HandleInittingGlobalVars.globalSettingsAssets;
        bool d10x2 = settings == null || settings.useD10Times2InsteadOfD20;
        if (d10x2)
            return Random.Range(1, 11) + Random.Range(1, 11);
        return Random.Range(1, 21);
    }

    static float HitChance(int mod, int ac, bool disadvantage)
    {
        float minRoll = ac - mod;
        float p = 21f - minRoll / 20f;
        if (disadvantage)
            p *= p;
        return Mathf.Clamp01(p);
    }

    public static bool HasWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.05f) return false;
        int mask = LayerMask.GetMask("Wall");
        if (Physics.Raycast(from, dir / dist, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            return true;
        return false;
    }
}

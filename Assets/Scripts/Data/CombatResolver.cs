using UnityEngine;

public static class CombatResolver
{
    public const float MeleeReach = 3f;

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
        p.isMelee = !attacker.HasRanged || p.distance <= MeleeReach;
        if (p.isMelee)
        {
            if (p.distance > MeleeReach + 0.05f)
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
        int mod = p.isMelee ? Mathf.RoundToInt(attacker.Strength) : Mathf.RoundToInt(attacker.Dexterity);
        int ac = Mathf.RoundToInt(target.ArmorClass);
        p.hitChance = HitChance(mod, ac, p.disadvantage);
        return p;
    }

    public static Result Resolve(PawnDataController attacker, PawnDataController target, Vector3 attackerPos, Vector3 targetPos)
    {
        Preview p = GetPreview(attacker, target, attackerPos, targetPos);
        Result r = new Result();
        r.canAttack = p.canAttack;
        r.isMelee = p.isMelee;
        r.hitChance = p.hitChance;
        r.blockMessage = p.blockMessage;
        if (!p.canAttack) return r;

        int mod = p.isMelee ? Mathf.RoundToInt(attacker.Strength) : Mathf.RoundToInt(attacker.Dexterity);
        int ac = Mathf.RoundToInt(target.ArmorClass);
        int d20a = Random.Range(1, 21);
        int d20b = p.disadvantage ? Random.Range(1, 21) : d20a;
        int roll = p.disadvantage ? Mathf.Min(d20a, d20b) : d20a;
        if (roll == 1)
        {
            r.hit = false;
            return r;
        }
        r.crit = roll == 20;
        r.hit = r.crit || roll + mod >= ac;
        if (!r.hit) return r;
        float dmg = p.isMelee ? attacker.RollMeleeDamage() : attacker.RollRangedDamage();
        if (r.crit) dmg *= 2f;
        r.damage = dmg;
        return r;
    }

    static float HitChance(int mod, int ac, bool disadvantage)
    {
        if (!disadvantage)
        {
            int hits = 0;
            for (int a = 1; a <= 20; a++)
            {
                if (a == 1) continue;
                if (a == 20 || a + mod >= ac) hits++;
            }
            return hits / 20f;
        }
        int ways = 0;
        for (int a = 1; a <= 20; a++)
        {
            for (int b = 1; b <= 20; b++)
            {
                int roll = Mathf.Min(a, b);
                if (roll == 1) continue;
                if (roll == 20 || roll + mod >= ac) ways++;
            }
        }
        return ways / 400f;
    }

    static bool HasWallBetween(Vector3 from, Vector3 to)
    {
        Vector3 a = from + Vector3.up * 1.2f;
        Vector3 b = to + Vector3.up * 1.2f;
        Vector3 dir = b - a;
        float dist = dir.magnitude;
        if (dist < 0.05f) return false;
        int mask = LayerMask.GetMask("Wall");
        if (Physics.Raycast(a, dir / dist, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            return true;
        return false;
    }
}

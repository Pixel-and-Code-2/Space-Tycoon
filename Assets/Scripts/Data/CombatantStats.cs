using UnityEngine;

[CreateAssetMenu(fileName = "CombatantStats", menuName = "SpaceTycoon/Combatant Stats", order = 1)]
public class CombatantStats : ScriptableObject
{
    public string displayName;
    public string weaponName;

    [Tooltip("Move meters per turn / free-roam budget base")]
    public string movePerTurn = "9";
    public string maxHp = "30";
    public string strength = "0";
    public string dexterity = "0";
    public string armorClass = "10";
    public string meleeDamage = "1d6";
    public string rangedDamage = "";
    [Tooltip("Max attack range without disadvantage")]
    public string attackRange = "1";

    public float RollMove() => DiceExpr.Roll(movePerTurn);
    public float RollMaxHp() => DiceExpr.Roll(maxHp);
    public float RollStrength() => DiceExpr.Roll(strength);
    public float RollDexterity() => DiceExpr.Roll(dexterity);
    public float RollArmorClass() => DiceExpr.Roll(armorClass);
    public float RollMeleeDamage() => DiceExpr.Roll(meleeDamage);
    public float RollRangedDamage() => string.IsNullOrWhiteSpace(rangedDamage) ? RollMeleeDamage() : DiceExpr.Roll(rangedDamage);
    public float RollAttackRange() => DiceExpr.Roll(attackRange);

    public bool HasRanged => !string.IsNullOrWhiteSpace(rangedDamage);
}

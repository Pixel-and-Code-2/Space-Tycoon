using UnityEngine;

public enum EnemyAiRole
{
    Melee,
    Shooter,
    Tank
}

[CreateAssetMenu(fileName = "EnemyAiProfile", menuName = "Space-Tycoon/Enemy AI Profile", order = 2)]
public class EnemyAiProfile : ScriptableObject
{
    public EnemyAiRole role = EnemyAiRole.Melee;

    [Header("Shared")]
    public bool useStaminaGate = true;
    public float minStaminaToAttack = 50f;
    public bool skipAttackOnDisadvantage = true;
    public bool useFinisher = true;
    public float finisherHpFraction = 0.5f;
    public bool skipAttackAfterMove = true;
    public bool allowDoubleAttack = true;
    [Range(0f, 1f)]
    public float attackAfterMoveChance = 0.45f;
    [Range(0f, 1f)]
    public float disadvantageAttackChance = 0.45f;

    [Header("Melee")]
    public float meleeCloseBandMin = 2f;
    public float meleeCloseBandMax = 4f;

    [Header("Shooter")]
    public float minShootDistance = 5f;
    public float retreatDistance = 9f;
    public float retreatMinPath = 8f;
    public float retreatMaxPath = 10f;
    public float zayaThreatDistance = 4f;
    public float zayaMeleeDistance = 1.25f;
    public float maxShootWithoutDisadvantage = 10f;

    [Header("Target pick weights (GDD %)")]
    public float weightPistol = 30f;
    public float weightMeleeAlly = 50f;
    public float weightRifle = 20f;
    public float weightRifleForShooter = 40f;
    public float weightPistolForShooter = 40f;
    public float weightMeleeAllyForShooter = 20f;
}

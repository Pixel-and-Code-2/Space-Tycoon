using UnityEngine;

public enum EnemyAiRole
{
    Melee,
    Shooter,
    Tank
}

[CreateAssetMenu(fileName = "EnemyAiProfile", menuName = "SpaceTycoon/Enemy AI Profile", order = 2)]
public class EnemyAiProfile : ScriptableObject
{
    public EnemyAiRole role = EnemyAiRole.Melee;

    [Header("Shared")]
    public bool useStaminaGate = true;
    public float minStaminaToAttack = 60f;
    public bool skipAttackOnDisadvantage = true;
    public bool useFinisher = true;
    public float finisherHpFraction = 0.2f;
    public bool skipAttackAfterMove = true;

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

    [Header("Target priority weights (lower = higher priority)")]
    public int priorityPistol = 0;
    public int priorityMeleeAlly = 1;
    public int priorityRifle = 2;
    public int priorityRifleForShooter = 0;
    public int priorityPistolForShooter = 1;
    public int priorityMeleeAllyForShooter = 2;
}

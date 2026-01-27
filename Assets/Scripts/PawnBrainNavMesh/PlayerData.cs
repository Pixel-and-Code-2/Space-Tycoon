using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "PlayerData", order = 1)]
public class PlayerData : ScriptableObject
{
    [Header("Active Parameters")]
    [SerializeField, Tooltip("max distance from mouse to walkable area, to show path")]
    public float maxSampleDistance = 5f;
    // less than 0 - unlimited
    [SerializeField, Range(-1f, float.MaxValue), Tooltip("less than 0 - unlimited")]
    public float availableDistance = -1f;

    [Header("Still in development")]
    public float health = 100f;
    public float strength = 100f;
    public float obstaclePushForce = 10f;
    public float verticalPushOverride = 0.2f;
}
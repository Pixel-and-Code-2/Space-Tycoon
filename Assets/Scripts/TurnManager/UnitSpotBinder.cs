using UnityEngine;

public class UnitSpotBinder : MonoBehaviour
{
    public enum Mode
    {
        SceneEnemy,
        Quarantine
    }

    public static System.Action<UnitSpotBinder> EditorValidateHook;

    [SerializeField]
    Mode mode = Mode.SceneEnemy;
    [SerializeField]
    GameObject enemyPrefab;
    [SerializeField]
    CombatantStats combatantStats;
    [Tooltip("Set Y=0 (NavMesh height on this map)")]
    [SerializeField]
    bool snapHeightToNavMesh = true;
    [SerializeField]
    float navMeshSampleDistance = 4f;
    [SerializeField]
    WarFog warFogOverride;
    [SerializeField]
    GameObject triggerObjectOverride;
    [SerializeField]
    GameObject spawnedInstance;
    [SerializeField]
    bool bound;

    public Mode SpotMode => mode;
    public GameObject EnemyPrefab => enemyPrefab;
    public CombatantStats CombatantStats => combatantStats;
    public bool SnapHeightToNavMesh => snapHeightToNavMesh;
    public float NavMeshSampleDistance => navMeshSampleDistance;
    public WarFog WarFogOverride => warFogOverride;
    public GameObject TriggerObjectOverride => triggerObjectOverride;
    public GameObject SpawnedInstance => spawnedInstance;
    public bool Bound => bound;
    public Transform Anchor => transform;

    public void EditorSetBound(GameObject instance, bool isBound)
    {
        spawnedInstance = instance;
        bound = isBound;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        EditorValidateHook?.Invoke(this);
    }
#endif
}

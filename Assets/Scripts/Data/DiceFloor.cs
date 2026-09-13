using UnityEngine;
using System.Collections;

public class DiceFloor : MonoBehaviour
{
    public static DiceFloor Instance { get; private set; }

    [SerializeField]
    private BoxCollider floorCollider;
    [SerializeField]
    private Vector2 size = new Vector2(200f, 200f);
    [SerializeField]
    private float thickness = 0.5f;

    void Awake()
    {
        Instance = this;
        if (floorCollider == null)
            floorCollider = GetComponent<BoxCollider>();
        if (floorCollider == null)
            floorCollider = gameObject.AddComponent<BoxCollider>();
        SyncFromSettings();
    }

    void OnEnable()
    {
        SyncFromSettings();
    }

    public void SyncFromSettings()
    {
        if (floorCollider == null)
            floorCollider = GetComponent<BoxCollider>();
        if (floorCollider == null)
            floorCollider = gameObject.AddComponent<BoxCollider>();
        float y = 0f;
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            y = HandleInittingGlobalVars.globalSettingsAssets.diceFloorY;
        transform.position = new Vector3(0f, y - thickness * 0.5f, 0f);
        floorCollider.size = new Vector3(size.x, thickness, size.y);
        floorCollider.center = Vector3.zero;
    }
}

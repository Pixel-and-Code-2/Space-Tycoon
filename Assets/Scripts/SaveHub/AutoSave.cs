using UnityEngine;

[RequireComponent(typeof(SaveHub))]
public class AutoSave : MonoBehaviour
{
    [SerializeField]
    private float intervalSeconds = 120f;
    private float nextSaveAt;

    void OnEnable()
    {
        nextSaveAt = Time.unscaledTime + intervalSeconds;
    }

    void Update()
    {
        if (Time.unscaledTime < nextSaveAt) return;
        nextSaveAt = Time.unscaledTime + intervalSeconds;
        TryAutosave();
    }

    void TryAutosave()
    {
        if (SaveHub.Instance == null) return;
        if (SaveHub.DEFAULT_SAVE_SLOT < 1) return;
        if (SaveHub.Instance.IsLoading) return;
        if (UILayersController.Instance == null) return;
        if (UILayersController.Instance.overlayStack == null || UILayersController.Instance.overlayStack.Count == 0) return;
        if (UILayersController.Instance.overlayStack.Peek() != UILayersController.UILayer.GameUI) return;
        SettingApplier.SaveSlot(SaveHub.DEFAULT_SAVE_SLOT);
        SaveHub.Instance.MakeSave(SaveHub.DEFAULT_SAVE_SLOT);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class ExitConfirmation : IUILayer
{
    [SerializeField]
    private InputActionReference returnToPauseButton;

    void OnEnable()
    {
        returnToPauseButton.action.Enable();
        gameObject.SetActive(true);
    }
    void OnDisable()
    {
        returnToPauseButton.action.Disable();
        gameObject.SetActive(false);
    }
    void Update()
    {
        if (returnToPauseButton.action.triggered)
        {
            OnReturnToPause();
        }
    }
    public void OnReturnToPause()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.PauseMenu);
    }
    public override void OnBackgroundClick()
    {
        OnReturnToPause();
    }
    public void OnExitWithSave()
    {
        if (SaveHub.DEFAULT_SAVE_SLOT == -1)
        {
            Debug.LogError("Save slot is not set");
            return;
        }
        SettingApplier.SaveSlot();
        SaveHub.Instance.MakeSave(SaveHub.DEFAULT_SAVE_SLOT);
        UILayersController.Instance.SetLayer(UILayersController.UILayer.MainMenu);
    }
    public void OnExitWithoutSave()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.MainMenu);
    }
}
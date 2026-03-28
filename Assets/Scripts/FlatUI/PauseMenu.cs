using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : IUILayer
{
    [SerializeField]
    private InputActionReference returnToGameButton;

    void OnEnable()
    {
        returnToGameButton.action.Enable();
        gameObject.SetActive(true);
    }
    void OnDisable()
    {
        returnToGameButton.action.Disable();
        gameObject.SetActive(false);
    }
    void Update()
    {
        if (returnToGameButton.action.triggered)
        {
            OnResume();
        }
    }

    public void OnResume()
    {
        UILayersController.Instance.GoBack();
    }
    public void OnTryMainMenu()
    {
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.ExitConfirmation);
    }
    public void OnSaveGame()
    {
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.SaveGame, "gameSave");
    }
    public void OnSettings()
    {
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.Settings);
    }
}
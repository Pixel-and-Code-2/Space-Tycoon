using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsMenu : IUILayer
{
    [SerializeField]
    private InputActionReference returnToPauseButton;
    private bool isMainMenu;
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
            OnBack();
        }
    }
    public override void OnBackgroundClick()
    {
        OnBack();
    }
    public void OnBack()
    {
        UILayersController.Instance.SetLayer(isMainMenu ? UILayersController.UILayer.MainMenu : UILayersController.UILayer.PauseMenu);
    }
    public override void Initialize(string config) // "mainMenu" or "pauseMenu"
    {
        isMainMenu = config == "mainMenu";
    }
    public void OnChangeControls(int variantNumber)
    {

    }
}
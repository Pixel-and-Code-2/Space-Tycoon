using UnityEngine;
using UnityEngine.InputSystem;

public class SaveMenu : IUILayer
{
    [SerializeField]
    private InputActionReference returnToPauseButton;
    private bool isGameStart;
    public override void Initialize(string config) // "gameStart" or "gameSave"
    {
        isGameStart = config == "gameStart";
    }
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
        UILayersController.Instance.SetLayer(isGameStart ? UILayersController.UILayer.MainMenu : UILayersController.UILayer.PauseMenu);
    }
    public void OnSaveGame(int slot)
    {
        if (isGameStart)
        {
            UILayersController.Instance.SetLayer(UILayersController.UILayer.GameUI);
            SaveHub.DEFAULT_SAVE_SLOT = slot;
            LoadGame(slot);
        }
        else
        {
            UILayersController.Instance.SetLayer(UILayersController.UILayer.PauseMenu);
            SaveGame(slot);
        }
    }
    private void SaveGame(int slot) { SaveHub.Instance.MakeSave(slot); }
    private void LoadGame(int slot) { SaveHub.Instance.LoadAllData(slot); }
}
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SaveMenu : IUILayer
{
    [SerializeField]
    private InputActionReference returnToPauseButton;
    private bool isGameStart;
    public static string GetSlotName(int slot) => "Slot" + slot;
    [SerializeField]
    private TextMeshProUGUI[] slotNames;
    public override void Initialize(string config) // "gameStart" or "gameSave"
    {
        isGameStart = config == "gameStart";
    }
    void OnEnable()
    {
        returnToPauseButton.action.Enable();
        gameObject.SetActive(true);
        UpdateButtons();
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
            SettingApplier.SaveSlot(slot);
            UILayersController.Instance.SetLayer(UILayersController.UILayer.PauseMenu);
            SaveGame(slot);
        }
    }
    public void OnDeleteSave(int slot)
    {
        PlayerPrefs.DeleteKey(GetSlotName(slot));
        SaveHub.Instance.ClearSaveData(slot);
        UpdateButtons();
    }
    private void SaveGame(int slot) { SaveHub.Instance.MakeSave(slot); }
    private void LoadGame(int slot) { SaveHub.Instance.LoadAllData(slot); }
    private void UpdateButtons()
    {
        for (int i = 0; i < 5; i++)
        {
            slotNames[i].text = PlayerPrefs.GetString(GetSlotName(i + 1), "Слот " + (i + 1) + ": Пусто");
        }
    }
}
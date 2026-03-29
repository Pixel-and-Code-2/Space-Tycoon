using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SaveMenu : IUILayer
{
    [SerializeField]
    private InputActionReference returnToPauseButton;
    private bool isGameStart;
    public static string GetSlotName(int slot) => "Slot" + slot;
    static string EmptySlotLabel(int slot) => "Слот " + slot + ": Пусто";

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
        UILayersController.Instance.GoBack();
    }
    public void OnSaveGame(int slot)
    {
        if (isGameStart)
        {
            SaveHub.DEFAULT_SAVE_SLOT = slot;
            string emptyLabel = EmptySlotLabel(slot);
            string slotName = PlayerPrefs.GetString(GetSlotName(slot), emptyLabel);
            if (string.Equals(slotName.TrimEnd(), emptyLabel, StringComparison.Ordinal))
            {
                UILayersController.Instance.SetLayer(UILayersController.UILayer.SlideShow, SlideShow.slidesDictionary[SlideShowType.Start]);
            }
            else
            {
                UILayersController.Instance.SetLayer(UILayersController.UILayer.GameUI);
            }
            LoadGame(slot);
        }
        else
        {
            SettingApplier.SaveSlot(slot);
            UILayersController.Instance.GoBack();
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
            slotNames[i].text = PlayerPrefs.GetString(GetSlotName(i + 1), EmptySlotLabel(i + 1));
        }
    }
}
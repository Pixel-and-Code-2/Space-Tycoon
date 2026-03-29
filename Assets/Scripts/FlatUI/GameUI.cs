using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

[DefaultExecutionOrder(-100)]
public class GameUI : IUILayer
{
    public static GameUI Instance { get; private set; }
    [SerializeField]
    private InputActionReference togglePause;
    [SerializeField]
    private TaskTextStyleChanger mainTaskTextStyleChanger;
    [SerializeField]
    private List<TaskTextStyleChanger> sideTaskTextStyleChangers;
    public override bool isStoppingGame => false;
    [SerializeField]
    private SliderController weaponSlider;
    [SerializeField]
    private TextMeshProUGUI weaponSliderText;
    [System.Serializable]
    private class PlayerGroupUIGameobjects
    {
        public IconButtonStyleFiller playerIcon;
        public SliderController playerSlider;
    }
    [System.Serializable]
    private class PlayerGroup
    {
        public string playerName;
        public string playerStyleName;
        public IControlableSelectable playerObject;
    }
    [Header("Player icons control")]
    [SerializeField]
    private List<PlayerGroupUIGameobjects> playerUIGameobjects;
    [SerializeField]
    private List<PlayerGroup> playerGroups;
    [SerializeField]
    private TextMeshProUGUI selectedPlayerName;
    [SerializeField]
    private TextMeshProUGUI selectedPlayerHealingNumber;
    private IControlableSelectable selectedPlayer = null;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Constructor met second GameUI instance");
        }
    }
    void Start()
    {
        ClickableItemsController.Instance.OnTaskUpdated += OnTaskUpdated;
        UILayersController.Instance.OnGameResumed += OnGameResumed;
    }
    void OnDestroy()
    {
        ClickableItemsController.Instance.OnTaskUpdated -= OnTaskUpdated;
        if (UILayersController.Instance != null)
            UILayersController.Instance.OnGameResumed -= OnGameResumed;
    }
    void OnGameResumed()
    {
        if (togglePause != null && togglePause.action != null)
            togglePause.action.Enable();
    }
    void OnEnable()
    {
        gameObject.SetActive(true);
        togglePause.action.Enable();
    }
    void OnDisable()
    {
        gameObject.SetActive(false);
        togglePause.action.Disable();
    }
    void Update()
    {
        if (selectedPlayer != PawnController.Instance.currentSelectedPawn)
        {
            selectedPlayer = PawnController.Instance.currentSelectedPawn;
            UpdateSelectedPlayer();
        }
        if (!togglePause.action.triggered)
            return;
        if (UILayersController.Instance.overlayStack.Peek() != UILayersController.UILayer.GameUI)
            return;
        if (PawnController.Instance != null && PawnController.Instance.currentSelectedPawn != null)
            return;
        OnPause();
    }
    public void OnPause()
    {
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.PauseMenu);
    }
    public void OnHelp()
    {
        // UILayersController.Instance.SetLayer(UILayersController.UILayer.HelpMenu);
    }
    private void OnTaskUpdated()
    {
        ClickableItemsController.TaskItem mainTask = null;
        var scenario = ClickableItemsController.Instance.mainTaskScenario;
        for (int i = scenario.Count - 1; i >= 0; i--)
        {
            if (scenario[i].status != ClickableItemsController.TaskItem.TaskItemStatus.Done)
            {
                mainTask = scenario[i];
                break;
            }
        }

        if (mainTask != null)
        {
            UpdateButton(mainTaskTextStyleChanger, mainTask);
        }
        else
        {
            mainTaskTextStyleChanger.ClearText();
            UILayersController.Instance.ShowOverlay(UILayersController.UILayer.SlideShow, SlideShow.slidesDictionary[SlideShowType.Win]);
        }
        int subTaskIndex = 0;
        for (int i = 0; i < scenario.Count && subTaskIndex < sideTaskTextStyleChangers.Count; i++)
        {
            var item = scenario[i];
            if (item == mainTask || item.status == ClickableItemsController.TaskItem.TaskItemStatus.Done)
                continue;

            UpdateButton(sideTaskTextStyleChangers[subTaskIndex], item);
            subTaskIndex++;
        }

        for (int i = subTaskIndex; i < sideTaskTextStyleChangers.Count; i++)
        {
            sideTaskTextStyleChangers[i].ClearText();
        }
    }
    private void UpdateButton(TaskTextStyleChanger button, ClickableItemsController.TaskItem item)
    {
        int isAvailable = -1;
        if (item.status == ClickableItemsController.TaskItem.TaskItemStatus.Unavailable) isAvailable = 0;
        else if (item.status == ClickableItemsController.TaskItem.TaskItemStatus.ReadyToStart) isAvailable = 1;
        button.ChangeText(
            item.shortLevelName,
            isAvailable,
            item.status == ClickableItemsController.TaskItem.TaskItemStatus.Done ? 1 : -1,
            item.status == ClickableItemsController.TaskItem.TaskItemStatus.InProgress ? 1 : -1
        );
    }
    public void OnChangeStats()
    {
        if (PawnController.Instance.currentSelectedPawn == null) return;
        float curMag = PawnController.Instance.currentSelectedPawn.GetDynamicParameterValue(PawnDataController.MAG_AMOUNT_KEY);
        float initMag = PawnController.Instance.currentSelectedPawn.GetDynamicParameterValue(PawnDataController.INITIAL_MAG_AMOUNT_KEY);
        float curAmmo = PawnController.Instance.currentSelectedPawn.GetDynamicParameterValue(PawnDataController.TOTAL_AMMO_KEY);
        weaponSlider.SetBounds(0f, initMag);
        weaponSlider.SetValue(curMag);
        weaponSliderText.text = curMag.ToString() + "/" + curAmmo.ToString();
    }

    private void UpdateSelectedPlayer()
    {
        if (selectedPlayer == null) return;

        for (int i = 1; i < playerGroups.Count; i++)
        {
            if (playerGroups[i].playerObject == selectedPlayer)
            {
                var temp = playerGroups[0];
                playerGroups[0] = playerGroups[i];
                playerGroups[i] = temp;
                break;
            }
        }
        UpdateUI();
    }
    private void UpdateUI()
    {
        var mainPlayerGroup = playerGroups[0];
        playerUIGameobjects[0].playerIcon.UpdateStyle(mainPlayerGroup.playerStyleName);
        float amountOfHealings = mainPlayerGroup.playerObject.GetDynamicParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY);
        float maxHealings = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
        selectedPlayerHealingNumber.text = (maxHealings - amountOfHealings).ToString();
        selectedPlayerName.text = mainPlayerGroup.playerName;
        for (int i = 1; i < playerGroups.Count; i++)
        {
            playerUIGameobjects[i].playerIcon.UpdateStyle(playerGroups[i].playerStyleName);
        }
        UpdatePlayersSlider();
    }
    public void SelectPlayer(int ind)
    {
        if (playerGroups[ind].playerObject.GetSelectableType() != SelectableType.Player) return;
        InputScreenMouseControlActions.Instance.SelectPlayer(playerGroups[ind].playerObject);
        ControlsVariantEasy.Instance.SelectPlayer(playerGroups[ind].playerObject);
    }
    private void UpdatePlayersSlider()
    {

        for (int i = 0; i < playerGroups.Count; i++)
        {
            var playerGroup = playerGroups[i];
            float initialDistance = playerGroup.playerObject.GetDynamicParameterValue(PawnDataController.INITIAL_AVAILABLE_DISTANCE_KEY);
            float availableDistance = playerGroup.playerObject.GetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY);
            playerUIGameobjects[i].playerSlider.SetBounds(0f, initialDistance);
            playerUIGameobjects[i].playerSlider.SetValue(availableDistance);
        }
    }
    public void UpdatePlayerData()
    {
        for (int i = 0; i < playerGroups.Count; i++)
        {
            if (playerGroups[i].playerObject.GetSelectableType() != SelectableType.Player)
            {
                if (playerUIGameobjects[i].playerIcon.IsButtonOn)
                {
                    playerUIGameobjects[i].playerIcon.TurnOffButton();
                    Debug.Log("turning off button for " + playerGroups[i].playerName);
                }
            }
            else
            {
                if (!playerUIGameobjects[i].playerIcon.IsButtonOn)
                {
                    playerUIGameobjects[i].playerIcon.TurnOnButton();
                    Debug.Log("turning on button for " + playerGroups[i].playerName);
                }
            }
        }
        UpdatePlayersSlider();
    }
}
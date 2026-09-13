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
    private TaskRowController mainTaskController;
    [SerializeField]
    private List<TaskRowController> sideTaskController;
    public override bool isStoppingGame => false;
    [SerializeField]
    private SliderController weaponSlider;
    [SerializeField]
    private TextMeshProUGUI weaponSliderText;
    [System.Serializable]
    public class PlayerGroup
    {
        public IControlableSelectable playerObject;
        public PlayerIconController playerIcon;
    }
    [Header("Player icons control")]
    [SerializeField]
    private List<PlayerGroup> playerGroups;
    private IControlableSelectable selectedPlayer = null;
    [Header("Background settings")]
    [SerializeField]
    private GameObject battleBackgroundObject;
    [SerializeField]
    private GameObject controlBarObject;
    [SerializeField]
    private TextMeshProUGUI weaponNameText;
    [SerializeField]
    private string shootingWeaponName = "ТКБ-К";
    [SerializeField]
    private string meleeWeaponName = "ИТО 40000";
    [SerializeField]
    private GameObject shootingObject;
    [SerializeField]
    private GameObject meleeObject;

    private SpriteProvider shootOutlineProvider;
    private SpriteProvider shootMaskProvider;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Constructor met second GameUI instance");
        }
        CacheWeaponProviders();
    }

    void CacheWeaponProviders()
    {
        if (shootingObject == null) return;
        var providers = shootingObject.GetComponentsInChildren<SpriteProvider>(true);
        for (int i = 0; i < providers.Length; i++)
        {
            if (providers[i].gameObject.name == "WeaponOutline") shootOutlineProvider = providers[i];
            if (providers[i].gameObject.name == "WeaponMask") shootMaskProvider = providers[i];
        }
    }
    void Start()
    {
        ClickableItemsController.Instance.OnTaskUpdated += OnTaskUpdated;
        UILayersController.Instance.OnGameResumed += OnGameResumed;
        SaveHub.Instance.OnLoad += OnLoadData;
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
        ApplyStepByStepUi(IsStepByStepNow());
    }
    private void OnLoadData(LoadedData data)
    {
        HelpSlideService.ResetSession();
        bool step = data.GetData("IsStepByStep", HandleInittingGlobalVars.UNIQUE_ID, false);
        ApplyStepByStepUi(step);
    }

    static bool IsStepByStepNow()
    {
        string key = HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY;
        return HandleInittingGlobalVars.globalParameters != null
            && HandleInittingGlobalVars.globalParameters.parametersDict.TryGetValue(key, out float value)
            && value > 0.5f;
    }

    void ApplyStepByStepUi(bool stepByStep)
    {
        if (battleBackgroundObject != null)
            battleBackgroundObject.SetActive(stepByStep);
        if (controlBarObject != null)
            controlBarObject.SetActive(stepByStep);
    }

    public PlayerIconController FindPlayerIcon(IControlableSelectable pawn)
    {
        if (pawn == null || playerGroups == null) return null;
        for (int i = 0; i < playerGroups.Count; i++)
        {
            if (playerGroups[i].playerObject == pawn)
                return playerGroups[i].playerIcon;
        }
        return null;
    }

    public int GetSelectedPlayerIndex()
    {
        if (playerGroups == null || PawnController.Instance == null) return -1;
        IControlableSelectable sel = PawnController.Instance.currentSelectedPawn;
        if (sel == null) return -1;
        for (int i = 0; i < playerGroups.Count; i++)
        {
            if (playerGroups[i].playerObject == sel)
                return i;
        }
        return -1;
    }

    public void ShowPlayerIconMessage(IControlableSelectable pawn, string message, Color color)
    {
        PlayerIconController icon = FindPlayerIcon(pawn);
        if (icon != null)
            icon.ShowPopup(message, color);
        else if (UI3DManager.Instance != null && pawn != null)
            UI3DManager.Instance.ShowMessage(message, pawn.GetTransform().position, color, true);
    }
    void OnEnable()
    {
        if (togglePause != null && togglePause.action != null)
            togglePause.action.Enable();
        StartCoroutine(OnTaskUpdatedDelayed());
    }
    void OnDisable()
    {
        gameObject.SetActive(false);
        if (togglePause != null && togglePause.action != null)
            togglePause.action.Disable();
    }
    void Update()
    {
        if (selectedPlayer != PawnController.Instance.currentSelectedPawn)
        {
            selectedPlayer = PawnController.Instance.currentSelectedPawn;
            UpdateSelectedPlayer();
            OnChangeStats();
        }
        if (togglePause == null || togglePause.action == null || !togglePause.action.triggered)
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
        if (!HelpSlideService.TryShowUnlockedUnion())
            UILayersController.Instance.ShowOverlay(UILayersController.UILayer.Help);
    }
    private System.Collections.IEnumerator OnTaskUpdatedDelayed()
    {
        yield return null;
        OnTaskUpdated();
        UpdateSelectedPlayer();
    }
    private void OnTaskUpdated()
    {
        ClickableItemsController.TaskItem mainTask = null;
        if (ClickableItemsController.Instance == null) return;
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
            mainTaskController.UpdateTask(mainTask);
        }
        else
        {
            mainTaskController.ClearText();
            StartCoroutine(EndGame());
        }
        int subTaskIndex = 0;
        for (int i = 0; i < scenario.Count && subTaskIndex < sideTaskController.Count; i++)
        {
            var item = scenario[i];
            if (item == mainTask || item.status == ClickableItemsController.TaskItem.TaskItemStatus.Done)
                continue;

            sideTaskController[subTaskIndex].UpdateTask(item);
            subTaskIndex++;
        }

        for (int i = 0; i < sideTaskController.Count; i++)
        {
            if (i < subTaskIndex)
            {
                if (!sideTaskController[i].gameObject.activeSelf)
                {
                    sideTaskController[i].gameObject.SetActive(true);
                }
                continue;
            }
            sideTaskController[i].ClearText();
            sideTaskController[i].gameObject.SetActive(false);
        }
    }
    private System.Collections.IEnumerator EndGame()
    {
        yield return new WaitForSeconds(2f);
        UI3DManager.Instance.HideContextMenu();
        UILayersController.Instance.SetLayer(UILayersController.UILayer.CutScene, "win");
    }
    public void OnChangeStats()
    {
        if (PawnController.Instance.currentSelectedPawn == null) return;
        if (weaponSlider != null)
        {
            weaponSlider.gameObject.SetActive(false);
        }
        if (weaponSliderText != null)
            weaponSliderText.text = "";
    }
    private void UpdateSelectedPlayer()
    {
        int selectedInd = -1;
        for (int i = 0; i < playerGroups.Count; i++)
        {
            PlayerIconState state = PlayerIconState.NotSelected;
            if (playerGroups[i].playerObject == null || playerGroups[i].playerObject.GetSelectableType() != SelectableType.Player)
            {
                state = PlayerIconState.Disable;
            }
            else if (playerGroups[i].playerObject == selectedPlayer)
            {
                state = PlayerIconState.Selected;
            }
            if (playerGroups[i].playerIcon != null)
            {
                playerGroups[i].playerIcon.UpdatePlayer(playerGroups[i]);
                playerGroups[i].playerIcon.UpdateState(state);
            }
            if (state == PlayerIconState.Selected)
            {
                selectedInd = i;
            }
        }
        if (selectedInd != -1)
        {
            ApplyWeaponBar(selectedInd);
        }
    }

    void ApplyWeaponBar(int selectedInd)
    {
        if (selectedInd == 0)
        {
            weaponNameText.text = meleeWeaponName;
            shootingObject.SetActive(false);
            meleeObject.SetActive(true);
            return;
        }

        shootingObject.SetActive(true);
        meleeObject.SetActive(false);
        if (selectedInd == 1)
        {
            weaponNameText.text = shootingWeaponName;
            if (shootOutlineProvider != null) shootOutlineProvider.SetLinks("SniperWeaponOutline", "UIMainColor");
            if (shootMaskProvider != null) shootMaskProvider.SetLinks("SniperWeaponMask", "UIWeaponMaskColor");
        }
        else
        {
            weaponNameText.text = shootingWeaponName;
            if (shootOutlineProvider != null) shootOutlineProvider.SetLinks("PistolWeaponOutline", "UIMainColor");
            if (shootMaskProvider != null) shootMaskProvider.SetLinks("PistolWeaponMask", "UIWeaponMaskColor");
        }
    }
    public void SelectPlayer(int ind)
    {
        if (playerGroups[ind].playerObject.GetSelectableType() != SelectableType.Player) return;
        if (PawnController.Instance != null && PawnController.Instance.IsSelectionLockedToCurrentActor())
        {
            IControlableSelectable locked = PawnController.Instance.GetLockedActor();
            if (locked != null && playerGroups[ind].playerObject != locked) return;
        }
        InputScreenMouseControlActions.Instance.SelectPlayer(playerGroups[ind].playerObject);
        // ControlsVariantEasy.Instance.SelectPlayer(playerGroups[ind].playerObject);
    }
    public void UpdatePlayerData()
    {
        for (int i = 0; i < playerGroups.Count; i++)
        {
            if (playerGroups[i].playerObject == null || playerGroups[i].playerIcon == null) continue;
            playerGroups[i].playerIcon.UpdatePlayer(playerGroups[i]);
            if (playerGroups[i].playerObject.GetSelectableType() != SelectableType.Player)
            {
                playerGroups[i].playerIcon.UpdateState(PlayerIconState.Disable);
            }
            else
            {
                playerGroups[i].playerIcon.UpdateState(selectedPlayer == playerGroups[i].playerObject ? PlayerIconState.Selected : PlayerIconState.NotSelected);
            }
        }
    }
}
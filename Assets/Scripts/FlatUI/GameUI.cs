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
}
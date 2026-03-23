using UnityEngine;
using UnityEngine.InputSystem;

public class SettingApplier : MonoBehaviour
{
    public static SettingApplier Instance { get; private set; }
    [SerializeField]
    private InputActionReference changeControlsAction;
    [SerializeField]
    private ISelectorBrain selectorBrain1;
    [SerializeField]
    private ISelectorBrain selectorBrain2;
    [SerializeField]
    private GameObject controlsPanel1;
    [SerializeField]
    private GameObject controlsPanel2;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("SettingApplier already exists");
    }
    void Start()
    {
        if (!changeControlsAction.action.enabled)
        {
            changeControlsAction.action.Enable();
        }
        int initialMode = PlayerPrefs.GetInt("SelectedBrain", 1);
        if (initialMode == 1)
        {
            SelectBrain1();
        }
        else
        {
            SelectBrain2();
        }
    }

    void OnEnable()
    {
        if (!changeControlsAction.action.enabled)
        {
            changeControlsAction.action.Enable();
        }
    }
    void OnDisable()
    {
        if (changeControlsAction.action.enabled)
        {
            changeControlsAction.action.Disable();
        }
    }

    void Update()
    {
        if (changeControlsAction.action.triggered)
        {
            ToggleChangeControls();
        }
    }

    void ToggleChangeControls()
    {
        if (selectorBrain1.enabled)
        {
            SelectBrain2();
            PlayerPrefs.SetInt("SelectedBrain", 2);
        }
        else
        {
            SelectBrain1();
            PlayerPrefs.SetInt("SelectedBrain", 1);
        }
    }
    public void SelectBrain1()
    {
        selectorBrain2.enabled = false;
        selectorBrain1.enabled = true;
        if (PawnController.Instance.currentSelector == selectorBrain2)
        {
            PawnController.Instance.ChangeSelectorBrain(selectorBrain1);
        }
        PawnController.Instance.playerSelectorBrain = selectorBrain1;
        controlsPanel1.SetActive(true);
        controlsPanel2.SetActive(false);
    }
    public void SelectBrain2()
    {
        selectorBrain1.enabled = false;
        selectorBrain2.enabled = true;
        if (PawnController.Instance.currentSelector == selectorBrain1)
        {
            PawnController.Instance.ChangeSelectorBrain(selectorBrain2);
        }
        PawnController.Instance.playerSelectorBrain = selectorBrain2;
        controlsPanel1.SetActive(false);
        controlsPanel2.SetActive(true);
    }
}
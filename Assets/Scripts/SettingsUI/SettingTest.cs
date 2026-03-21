using UnityEngine;
using UnityEngine.InputSystem;

public class SettingTest : MonoBehaviour
{
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


    void Start()
    {
        if (!changeControlsAction.action.enabled)
        {
            changeControlsAction.action.Enable();
        }
        ToggleChangeControls();
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
        else
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
    }
}
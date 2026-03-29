using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NarrativeTextWindow : IUILayer
{
    [SerializeField]
    private TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    private InputActionReference returnToGameButton;
    private RectTransform parentRect;
    public override bool isStoppingGame => false;
    // public override bool isBackgroundVisible => false;
    [SerializeField]
    private float duration = 1f;
    private void Awake()
    {
        parentRect = GetComponent<RectTransform>();
    }
    void OnEnable()
    {
        returnToGameButton.action.Enable();
        gameObject.SetActive(true);
        timeElapsed = 0f;
    }
    void OnDisable()
    {
        returnToGameButton.action.Disable();
        gameObject.SetActive(false);
    }
    private float timeElapsed = 0f;
    void Update()
    {
        if (returnToGameButton.action.triggered)
        {
            UILayersController.Instance.GoBack();
        }
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= duration)
        {
            UILayersController.Instance.GoBack();
        }
    }
    public override void OnBackgroundClick()
    {
        UILayersController.Instance.GoBack();
    }
    public override void Initialize(string config)
    {
        textMeshProUGUI.text = config;
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }
}
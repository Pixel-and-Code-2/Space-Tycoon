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
    private void Awake()
    {
        parentRect = GetComponent<RectTransform>();
    }
    void OnEnable()
    {
        returnToGameButton.action.Enable();
        gameObject.SetActive(true);
    }
    void OnDisable()
    {
        returnToGameButton.action.Disable();
        gameObject.SetActive(false);
    }
    void Update()
    {
        if (returnToGameButton.action.triggered)
        {
            OnResume();
        }
    }
    public override void OnBackgroundClick()
    {
        OnResume();
    }
    public void OnResume()
    {
        UILayersController.Instance.GoBack();
    }
    public override void Initialize(string config)
    {
        textMeshProUGUI.text = config;
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }
}
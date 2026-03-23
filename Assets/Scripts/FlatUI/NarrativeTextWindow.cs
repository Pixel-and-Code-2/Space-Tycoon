using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class NarrativeTextWindow : IUILayer
{
    [SerializeField]
    private TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    private InputActionReference returnToGameButton;

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
        UILayersController.Instance.SetLayer(UILayersController.UILayer.GameUI);
    }
    public override void Initialize(string config)
    {
        textMeshProUGUI.text = config;
    }
}
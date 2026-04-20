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
    [SerializeField]
    private float[] yLevels = new float[] { 20f, 100f, 200f };
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
        string[] parts = config.Split('_');
        int parsedNumber;
        if (parts.Length > 1 && int.TryParse(parts[1], out parsedNumber) && parsedNumber >= 0 && parsedNumber <= 2) ;
        else parsedNumber = 0;
        textMeshProUGUI.text = parts[0];
        var rt = (RectTransform)transform;
        var p = rt.anchoredPosition;
        p.y = yLevels[parsedNumber];
        rt.anchoredPosition = p;
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }
}
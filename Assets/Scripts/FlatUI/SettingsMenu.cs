using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : IUILayer
{
    [SerializeField]
    private InputActionReference returnToPauseButton;
    private int tabNumber = 0;
    [SerializeField]
    private GameObject[] tabs;
    [SerializeField]
    private string control1SpriteName;
    [SerializeField]
    private string control2SpriteName;
    [SerializeField]
    private Image controlImage;
    private int currentControl = 1;
    private RectTransform rectTransform;
    [SerializeField]
    private TextMeshProUGUI controlNameText;
    [SerializeField]
    private TextMeshProUGUI controlDesritptionText;
    [SerializeField]
    private string control1Name;
    [SerializeField]
    private string control2Name;
    [SerializeField]
    private string control1Description;
    [SerializeField]
    private string control2Description;
    void Start()
    {
        currentControl = PlayerPrefs.GetInt("SelectedBrain", 1);
        OnValidate();
    }
    void OnValidate()
    {
        CheckControls(true);
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }
    void OnEnable()
    {
        returnToPauseButton.action.Enable();
        gameObject.SetActive(true);
    }
    void OnDisable()
    {
        returnToPauseButton.action.Disable();
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
    public void OnChangeControls(bool isPositive)
    {
        currentControl += isPositive ? 1 : -1;
        if (currentControl < 1) currentControl = 2;
        if (currentControl > 2) currentControl = 1;
        CheckControls();
    }
    private void CheckControls(bool onlySprite = false)
    {
        if (currentControl == 1)
        {
            if (!onlySprite) SettingApplier.Instance.SelectBrain1();
            if (HandleInittingGlobalVars.globalSettingsAssets == null) return;
            var spriteLink = HandleInittingGlobalVars.globalSettingsAssets.GetSpriteLink(control1SpriteName);
            if (spriteLink == null) return;
            controlImage.sprite = spriteLink.sprite;
            controlNameText.text = control1Name;
            controlDesritptionText.text = control1Description;
        }
        else if (currentControl == 2)
        {
            if (!onlySprite) SettingApplier.Instance.SelectBrain2();
            if (HandleInittingGlobalVars.globalSettingsAssets == null) return;
            var spriteLink = HandleInittingGlobalVars.globalSettingsAssets.GetSpriteLink(control2SpriteName);
            if (spriteLink == null) return;
            controlImage.sprite = spriteLink.sprite;
            controlNameText.text = control2Name;
            controlDesritptionText.text = control2Description;
        }
    }
    public void OnChangeTab(int tabNumber)
    {
        this.tabNumber = tabNumber;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].gameObject.SetActive(i == tabNumber);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}


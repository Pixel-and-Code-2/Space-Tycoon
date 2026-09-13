using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : IUILayer
{
    const string PhysicalDicePrefsKey = "UsePhysicalDice";

    [SerializeField]
    private InputActionReference returnToPauseButton;
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
    [SerializeField]
    private Slider musicSlider;
    [SerializeField]
    private Slider soundSlider;
    [SerializeField]
    private Toggle physicalDiceToggle;

    void Start()
    {
        currentControl = PlayerPrefs.GetInt("SelectedBrain", 1);
        OnValidate();
        soundSlider.value = PlayerPrefs.GetFloat("SoundVolume", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        ApplyPhysicalDiceFromPrefs();
        EnsurePhysicalDiceToggle();
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
        ApplyPhysicalDiceFromPrefs();
        EnsurePhysicalDiceToggle();
    }

    void OnDisable()
    {
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
            controlNameText.text = control1Name;
            controlDesritptionText.text = control1Description;
        }
        else if (currentControl == 2)
        {
            if (!onlySprite) SettingApplier.Instance.SelectBrain2();
            if (HandleInittingGlobalVars.globalSettingsAssets == null) return;
            controlNameText.text = control2Name;
            controlDesritptionText.text = control2Description;
        }
    }

    public void OnChangeTab(int tabNumber)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].gameObject.SetActive(i == tabNumber);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void OnChangeVolume(bool isMusic)
    {
        AudioController.Instance.SetVolume(isMusic ? musicSlider.value : soundSlider.value, isMusic);
        PlayerPrefs.SetFloat(isMusic ? "MusicVolume" : "SoundVolume", isMusic ? musicSlider.value : soundSlider.value);
    }

    public void OnPhysicalDiceChanged(bool on)
    {
        PlayerPrefs.SetInt(PhysicalDicePrefsKey, on ? 1 : 0);
        PlayerPrefs.Save();
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            HandleInittingGlobalVars.globalSettingsAssets.usePhysicalDice = on;
    }

    public static void ApplyPhysicalDiceFromPrefs()
    {
        bool on = PlayerPrefs.GetInt(PhysicalDicePrefsKey, 0) == 1;
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            HandleInittingGlobalVars.globalSettingsAssets.usePhysicalDice = on;
    }

    void EnsurePhysicalDiceToggle()
    {
        if (physicalDiceToggle == null && tabs != null && tabs.Length > 0 && tabs[0] != null)
        {
            Transform existing = tabs[0].transform.Find("PhysicalDiceToggle");
            if (existing != null)
                physicalDiceToggle = existing.GetComponent<Toggle>();
        }
        if (physicalDiceToggle == null && tabs != null && tabs.Length > 0 && tabs[0] != null)
            physicalDiceToggle = BuildPhysicalDiceToggle(tabs[0].transform);

        if (physicalDiceToggle == null) return;
        bool on = PlayerPrefs.GetInt(PhysicalDicePrefsKey, 0) == 1;
        physicalDiceToggle.onValueChanged.RemoveListener(OnPhysicalDiceChanged);
        physicalDiceToggle.isOn = on;
        physicalDiceToggle.onValueChanged.AddListener(OnPhysicalDiceChanged);
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            HandleInittingGlobalVars.globalSettingsAssets.usePhysicalDice = on;
    }

    static Toggle BuildPhysicalDiceToggle(Transform parent)
    {
        GameObject row = new GameObject("PhysicalDiceToggle", typeof(RectTransform), typeof(Toggle));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.sizeDelta = new Vector2(0f, 36f);
        rowRt.anchoredPosition = new Vector2(0f, -220f);

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(row.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.5f);
        bgRt.anchorMax = new Vector2(0f, 0.5f);
        bgRt.pivot = new Vector2(0f, 0.5f);
        bgRt.sizeDelta = new Vector2(28f, 28f);
        bgRt.anchoredPosition = new Vector2(16f, 0f);
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        check.transform.SetParent(bg.transform, false);
        RectTransform checkRt = check.GetComponent<RectTransform>();
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = new Vector2(4f, 4f);
        checkRt.offsetMax = new Vector2(-4f, -4f);
        Image checkImg = check.GetComponent<Image>();
        checkImg.color = new Color(0.35f, 0.85f, 0.4f, 1f);

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(row.transform, false);
        RectTransform labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(56f, 0f);
        labelRt.offsetMax = new Vector2(-8f, 0f);
        TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = "Кубик перед атакой";
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        Toggle toggle = row.GetComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = false;
        return toggle;
    }
}

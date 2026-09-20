using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmDialog : IUILayer
{
    public static ConfirmDialog Instance { get; private set; }

    [SerializeField]
    TextMeshProUGUI messageText;
    [SerializeField]
    Button yesButton;
    [SerializeField]
    Button noButton;

    Action onYes;
    Action onNo;

    public override bool isStoppingGame => true;

    void Awake()
    {
        Instance = this;
        if (yesButton != null) yesButton.onClick.AddListener(OnYes);
        if (noButton != null) noButton.onClick.AddListener(OnNo);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string message, Action yes, Action no = null)
    {
        if (Instance == null || UILayersController.Instance == null)
        {
            yes?.Invoke();
            return;
        }
        Instance.onYes = yes;
        Instance.onNo = no;
        if (Instance.messageText != null)
            Instance.messageText.text = message;
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.ConfirmDialog);
    }

    public void OnYes()
    {
        Action a = onYes;
        Clear();
        UILayersController.Instance.GoBack();
        a?.Invoke();
    }

    public void OnNo()
    {
        Action a = onNo;
        Clear();
        UILayersController.Instance.GoBack();
        a?.Invoke();
    }

    public override void OnBackgroundClick()
    {
        OnNo();
    }

    void Clear()
    {
        onYes = null;
        onNo = null;
    }
}

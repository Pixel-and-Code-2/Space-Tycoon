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
        EnsureWired();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string message, Action yes, Action no = null)
    {
        if (Instance == null)
        {
            ConfirmDialog found = UnityEngine.Object.FindFirstObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
            if (found != null)
            {
                Instance = found;
                found.EnsureWired();
            }
        }
        if (Instance == null || UILayersController.Instance == null)
        {
            Debug.LogWarning("[ConfirmDialog] missing instance/UILayers — cannot show confirm");
            return;
        }
        Instance.onYes = yes;
        Instance.onNo = no;
        if (Instance.messageText != null)
            Instance.messageText.text = message;
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.ConfirmDialog);
    }

    void EnsureWired()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYes);
        }
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNo);
        }
    }

    public void OnYes()
    {
        Action a = onYes;
        Clear();
        if (UILayersController.Instance != null)
            UILayersController.Instance.GoBack();
        a?.Invoke();
    }

    public void OnNo()
    {
        Action a = onNo;
        Clear();
        if (UILayersController.Instance != null)
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

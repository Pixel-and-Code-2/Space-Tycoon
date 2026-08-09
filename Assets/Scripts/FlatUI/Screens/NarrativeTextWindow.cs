using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class NarrativeTextWindow : IUILayer
{
    [System.Serializable]
    private class QueueItem
    {
        public string text;
        public int yLevel;
    }
    [SerializeField]
    private List<QueueItem> queueItems = new List<QueueItem>();
    public override bool isQueueable => true;
    [SerializeField]
    private TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    private InputActionReference returnToGameButton;
    private RectTransform parentRect;
    public override bool isStoppingGame => true;
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
        timeElapsed += Time.unscaledDeltaTime;
        if (timeElapsed >= duration)
        {
            UILayersController.Instance.GoBack();
        }
    }
    public override void OnBackgroundClick()
    {
        if (queueItems.Count > 0) {
            SetText(queueItems[0].text, queueItems[0].yLevel);
            queueItems.RemoveAt(0);
            Debug.Log("NarrativeTextWindow: Queue item removed: " + queueItems.Count);
        } else {
            UILayersController.Instance.GoBack();
            Debug.Log("NarrativeTextWindow: No queue items left, going back");
        }
    }
    public override void Initialize(string config)
    {
        (string text, int layer) = ParseConfig(config);
        SetText(text, layer);
    }
    private void SetText(string text, int layer) {
        textMeshProUGUI.text = text;
        var rt = (RectTransform)transform;
        var p = rt.anchoredPosition;
        p.y = yLevels[layer];
        rt.anchoredPosition = p;
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }
    public override void Queue(string config)
    {
        (string text, int layer) = ParseConfig(config);

        queueItems.Add(new QueueItem { text = text, yLevel = layer });
    }
    private (string, int) ParseConfig(string config)
    {
        string[] parts = config.Split('_');
        int parsedNumber;
        if (parts.Length <= 1 || !int.TryParse(parts[1], out parsedNumber) || parsedNumber < 0 || parsedNumber > 2) parsedNumber = 0;
        return (parts[0], parsedNumber);
    }
}
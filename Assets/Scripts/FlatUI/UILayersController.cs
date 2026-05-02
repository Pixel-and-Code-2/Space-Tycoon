using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class UILayersController : MonoBehaviour
{
    public static UILayersController Instance { get; private set; }
    public event Action OnGameResumed;
    public enum UILayer
    {
        Background = -2,
        GameUI = -1,
        PauseMenu = 0,
        Settings = 1,
        ExitConfirmation = 2,
        MainMenu = 3,
        SaveGame = 4,
        NarrativeText = 5,
        AttentionText = 6,
        SlideShow = 7,
        Help = 8,
        CutScene = 9
    }
    [System.Serializable]
    private class UILayerEntry
    {
        public UILayer layer;
        public IUILayer uiLayer;
    }
    [SerializeField]
    private List<UILayerEntry> layers;
    private Dictionary<UILayer, IUILayer> layersDictionary = new Dictionary<UILayer, IUILayer>();
    [SerializeField]
    private UILayer startLayer = UILayer.MainMenu;
    [System.Serializable]
    private class BackgroundObject
    {
        public GameObject dimScreenObject;
        public GameObject backgoundClickableObject;
    }
    [SerializeField]
    private List<BackgroundObject> backgroundObjects;
    public Stack<UILayer> overlayStack { get; private set; } = new Stack<UILayer>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("UILayersController instance already exists");
        OnValidate();
    }
    void OnValidate()
    {
        layersDictionary.Clear();
        foreach (var layer in layers)
        {
            if (layer.uiLayer != null)
            {
                layersDictionary.Add(layer.layer, layer.uiLayer);
            }
        }
    }

    void Start()
    {
        overlayStack.Push(startLayer);
        CheckLayersStack();
    }

    private void CheckLayersStack()
    {
        foreach (var layer in layers)
        {
            if (!overlayStack.Contains(layer.layer))
            {
                layer.uiLayer.gameObject.SetActive(false);
            }
        }
        if (overlayStack.Count == 0)
        {
            // Debug.LogWarning("Sth went wrong, no layers in stack, setting game ui");
            overlayStack.Push(UILayer.GameUI);
        }
        int avBgInd = 0;
        foreach (var layer in overlayStack.Reverse())
        {
            if (layer == UILayer.Background)
            {
                if (avBgInd < backgroundObjects.Count)
                {
                    backgroundObjects[avBgInd].dimScreenObject.SetActive(true);
                    backgroundObjects[avBgInd].backgoundClickableObject.SetActive(true);
                }
                else
                {
                    Debug.LogError("Error: not enough background objects");
                    continue;
                }
                backgroundObjects[avBgInd].dimScreenObject.gameObject.transform.SetAsLastSibling();
                backgroundObjects[avBgInd].backgoundClickableObject.gameObject.transform.SetAsLastSibling();
                avBgInd++;
                continue;
            }
            layersDictionary[layer].gameObject.SetActive(true);
            layersDictionary[layer].gameObject.transform.SetAsLastSibling();
        }
        if (avBgInd < backgroundObjects.Count - 1)
        {
            for (int i = avBgInd; i < backgroundObjects.Count; i++)
            {
                backgroundObjects[i].dimScreenObject.SetActive(false);
                backgroundObjects[i].backgoundClickableObject.SetActive(false);
            }
        }
        bool isStopping = false;
        foreach (var layer in overlayStack)
        {
            if (layer != UILayer.Background && layersDictionary[layer].isStoppingGame)
            {
                isStopping = true;
                break;
            }
        }
        if (isStopping) StopGame();
        else ResumeGame();
    }
    private void AddLayer(UILayer layer, string config = null)
    {
        if (overlayStack.Count > 0 && layersDictionary[layer].isBackgroundVisible)
        {
            overlayStack.Push(UILayer.Background);
        }
        overlayStack.Push(layer);
        CheckLayersStack();
        if (config != null && layersDictionary.ContainsKey(layer))
        {
            layersDictionary[layer].Initialize(config);
        }
    }

    public void SetLayer(UILayer layer, string config = null)
    {
        overlayStack.Clear();
        AddLayer(layer, config);
    }

    public void SetLayerKeepingGameUI(UILayer layer, string config = null)
    {
        overlayStack.Clear();
        overlayStack.Push(UILayer.GameUI);
        AddLayer(layer, config);
    }
    public void ShowOverlay(UILayer layer, string config = null)
    {
        AddLayer(layer, config);
    }
    public void GoBack()
    {
        overlayStack.Pop();
        while (overlayStack.Count > 0 && overlayStack.Peek() == UILayer.Background)
        {
            overlayStack.Pop();
        }
        CheckLayersStack();
    }
    private void StopGame()
    {
        Time.timeScale = 0f;
    }
    private void ResumeGame()
    {
        Time.timeScale = 1f;
        OnGameResumed?.Invoke();
    }
    public IUILayer GetLayer(UILayer layer)
    {
        return layersDictionary[layer];
    }
    public void OnBackgroundClick()
    {
        if (overlayStack.Peek() == UILayer.GameUI) return;
        layersDictionary[overlayStack.Peek()].OnBackgroundClick();
    }
}
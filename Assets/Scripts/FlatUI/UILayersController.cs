using UnityEngine;
using System.Collections.Generic;
using System;

public class UILayersController : MonoBehaviour
{
    public static UILayersController Instance { get; private set; }
    public event Action OnGameResumed;
    public enum UILayer
    {
        GameUI = -1,
        PauseMenu = 0,
        Settings = 1,
        ExitConfirmation = 2,
        MainMenu = 3,
        SaveGame = 4,
        NarrativeText = 5
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
    private UILayer startLayer = UILayer.PauseMenu;
    public UILayer currentLayer { get; private set; }
    [SerializeField]
    private GameObject dimScreenObject;
    [SerializeField]
    private GameObject backgoundClickableObject;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("UILayersController instance already exists");
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
        currentLayer = startLayer;
        CheckCurrentLayer();
    }

    private void CheckCurrentLayer()
    {
        foreach (var layer in layers)
        {
            if (layer.layer != currentLayer)
            {
                layer.uiLayer.gameObject.SetActive(false);
            }
        }
        ShowLayer(currentLayer);
        if (currentLayer != UILayer.GameUI) StopGame();
        else ResumeGame();
    }
    private void ShowLayer(UILayer layer)
    {
        layersDictionary[layer].gameObject.SetActive(true);
        if (layer != UILayer.GameUI)
        {
            dimScreenObject.SetActive(layersDictionary[layer].isBackgroundVisible);
        }
        else
        {
            dimScreenObject.SetActive(false);
            backgoundClickableObject.SetActive(false);
        }
    }

    public void SetLayer(UILayer layer, string config = null)
    {
        currentLayer = layer;
        CheckCurrentLayer();
        if (config != null && layersDictionary.ContainsKey(currentLayer))
        {
            layersDictionary[currentLayer].Initialize(config);
        }
    }
    private void StopGame()
    {
        backgoundClickableObject.SetActive(true);
        Time.timeScale = 0f;
    }
    private void ResumeGame()
    {
        backgoundClickableObject.SetActive(false);
        Time.timeScale = 1f;
        OnGameResumed?.Invoke();
    }
    public IUILayer GetLayer(UILayer layer)
    {
        return layersDictionary[layer];
    }
    public void OnBackgroundClick()
    {
        if (currentLayer == UILayer.GameUI) return;
        layersDictionary[currentLayer].OnBackgroundClick();
    }
}
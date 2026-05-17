using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(BoxCollider))]
public class WarFog : MonoBehaviour
{
    [SerializeField]
    private Material fogMaterial;
    public static event Action OnWarFogEnd;
    public static event Action OnWarFogStart;

    private Dictionary<Renderer, Material[]> rendererDefaultMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<GameObject, bool> excludedActiveState = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, int> objectOriginalLayers = new Dictionary<GameObject, int>();

    [SerializeField]
    private List<GameObject> othersToInclude;
    [SerializeField]
    private LayerMask excludedLayers;
    private string UNIQUE_ID => "WarFog_" + gameObject.name;
    private bool isHidden = true;

    void Awake()
    {
        RegisterSnapshot();
        HideEverything();
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

    void Start()
    {
        SaveHub.Instance.OnSave += OnSave;
        SaveHub.Instance.OnLoad += OnLoad;
    }

    private bool IsExcludedLayer(GameObject go)
    {
        if (go == null || excludedLayers.value == 0) return false;
        return ((1 << go.layer) & excludedLayers.value) != 0;
    }

    private void RegisterExcluded(GameObject go)
    {
        if (go == null || !IsExcludedLayer(go)) return;
        if (!excludedActiveState.ContainsKey(go))
            excludedActiveState[go] = go.activeSelf;
    }

    private void RegisterRenderer(Renderer renderer)
    {
        if (renderer == null) return;
        if (IsExcludedLayer(renderer.gameObject))
        {
            RegisterExcluded(renderer.gameObject);
            return;
        }
        if (rendererDefaultMaterials.ContainsKey(renderer))
            return;
        Material[] src = renderer.sharedMaterials;
        Material[] copy = new Material[src.Length];
        Array.Copy(src, copy, src.Length);
        rendererDefaultMaterials[renderer] = copy;
    }

    private void RegisterRenderersUnder(GameObject root)
    {
        if (root == null) return;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            RegisterRenderer(r);
        foreach (SkinnedMeshRenderer r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            RegisterRenderer(r);
    }

    private void RegisterSnapshot()
    {
        RegisterRenderersUnder(gameObject);
        if (othersToInclude == null) return;
        foreach (GameObject other in othersToInclude)
        {
            if (other == null) continue;
            if (!objectOriginalLayers.ContainsKey(other))
                objectOriginalLayers[other] = other.layer;
            if (IsExcludedLayer(other))
            {
                RegisterExcluded(other);
                continue;
            }
            RegisterRenderersUnder(other);
        }
    }

    private void OnSave(Action<SaveRecord[], string> save)
    {
        save(new SaveRecord[] {
            new SaveRecord(){
                recordName = "isHidden",
                recordType = SaveRecordType.boolean,
                boolValue = isHidden
            }
        }, UNIQUE_ID);
    }

    private void OnLoad(LoadedData loadedData)
    {
        bool newIsHidden = loadedData.GetData("isHidden", UNIQUE_ID, true);
        if (newIsHidden != isHidden)
        {
            if (newIsHidden)
                HideEverything();
            else
                ShowEverything();
        }
    }

    private void SetExcludedActive(bool active)
    {
        foreach (var pair in excludedActiveState)
        {
            if (pair.Key == null) continue;
            pair.Key.SetActive(active ? pair.Value : false);
        }
    }

    private void ApplyHiddenVisuals()
    {
        foreach (var pair in rendererDefaultMaterials)
        {
            if (pair.Key == null) continue;
            Material[] fogMats = new Material[pair.Value.Length];
            for (int i = 0; i < fogMats.Length; i++)
                fogMats[i] = fogMaterial;
            pair.Key.sharedMaterials = fogMats;
        }
        SetExcludedActive(false);
        int warFogLayer = LayerMask.NameToLayer("WarFog");
        if (othersToInclude == null) return;
        foreach (GameObject other in othersToInclude)
        {
            if (other == null || IsExcludedLayer(other)) continue;
            other.layer = warFogLayer;
        }
    }

    private void ApplyVisibleVisuals()
    {
        foreach (var pair in rendererDefaultMaterials)
        {
            if (pair.Key == null) continue;
            pair.Key.sharedMaterials = pair.Value;
        }
        SetExcludedActive(true);
        foreach (var pair in objectOriginalLayers)
        {
            if (pair.Key == null) continue;
            pair.Key.layer = pair.Value;
        }
    }

    private void HideEverything()
    {
        ApplyHiddenVisuals();
        isHidden = true;
        OnWarFogStart?.Invoke();
    }

    public void ShowEverything()
    {
        if (isHidden)
        {
            ApplyVisibleVisuals();
            isHidden = false;
            OnWarFogEnd?.Invoke();
        }
    }

    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoad;
            SaveHub.Instance.OnSave -= OnSave;
        }
    }
}

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
    private Dictionary<GameObject, int> objectOriginalLayers = new Dictionary<GameObject, int>();

    [SerializeField]
    private List<GameObject> othersToInclude;
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

    private void RegisterRenderer(Renderer renderer)
    {
        if (renderer == null || rendererDefaultMaterials.ContainsKey(renderer))
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
        int warFogLayer = LayerMask.NameToLayer("WarFog");
        if (othersToInclude == null) return;
        foreach (GameObject other in othersToInclude)
        {
            if (other == null) continue;
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
}

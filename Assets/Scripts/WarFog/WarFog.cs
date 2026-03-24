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

    private readonly Dictionary<Renderer, Material[]> rendererDefaultSharedMaterials = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<GameObject, int> objectOriginalLayers = new Dictionary<GameObject, int>();

    [SerializeField]
    private List<GameObject> othersToInclude;
    private string UNIQUE_ID => "WarFog_" + gameObject.name;

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

    private static Material[] CloneSharedMaterials(Renderer renderer)
    {
        Material[] src = renderer.sharedMaterials;
        Material[] dst = new Material[src.Length];
        Array.Copy(src, dst, src.Length);
        return dst;
    }

    private void RegisterRenderer(Renderer renderer)
    {
        if (renderer == null || rendererDefaultSharedMaterials.ContainsKey(renderer))
            return;
        rendererDefaultSharedMaterials[renderer] = CloneSharedMaterials(renderer);
    }

    private void RegisterRenderersUnder(GameObject root)
    {
        if (root == null)
            return;
        foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
            RegisterRenderer(meshRenderer);
        foreach (SkinnedMeshRenderer skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            RegisterRenderer(skinnedMeshRenderer);
    }

    private void RegisterSnapshot()
    {
        RegisterRenderersUnder(gameObject);
        if (othersToInclude == null)
            return;
        foreach (GameObject other in othersToInclude)
        {
            if (other == null)
                continue;
            RegisterRenderersUnder(other);
            if (!objectOriginalLayers.ContainsKey(other))
                objectOriginalLayers[other] = other.layer;
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
            isHidden = newIsHidden;
            if (newIsHidden)
            {
                HideEverything();
            }
            else
            {
                ShowEverything();
            }
        }
    }

    private void ApplyHiddenVisuals()
    {
        foreach (KeyValuePair<Renderer, Material[]> pair in rendererDefaultSharedMaterials)
        {
            Renderer renderer = pair.Key;
            if (renderer == null)
                continue;
            Material[] defaults = pair.Value;
            Material[] fogMats = new Material[defaults.Length];
            for (int i = 0; i < fogMats.Length; i++)
                fogMats[i] = fogMaterial;
            renderer.sharedMaterials = fogMats;
        }
        foreach (KeyValuePair<GameObject, int> pair in objectOriginalLayers)
        {
            GameObject go = pair.Key;
            if (go == null)
                continue;
            go.layer = LayerMask.NameToLayer("WarFog");
        }
    }

    private void ApplyVisibleVisuals()
    {
        foreach (KeyValuePair<Renderer, Material[]> pair in rendererDefaultSharedMaterials)
        {
            Renderer renderer = pair.Key;
            if (renderer == null)
                continue;
            renderer.sharedMaterials = pair.Value;
        }
        foreach (KeyValuePair<GameObject, int> pair in objectOriginalLayers)
        {
            GameObject go = pair.Key;
            if (go == null)
                continue;
            go.layer = pair.Value;
        }
    }

    private bool isHidden = true;

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

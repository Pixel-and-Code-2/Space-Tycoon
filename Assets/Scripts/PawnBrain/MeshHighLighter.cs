using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MeshHighLighter : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    [SerializeField]
    private Color highlightColor = Color.red;
    private Color highlightColorCache;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Highlight(Color color)
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.color = color;
    }

    void OnValidate()
    {
        if (highlightColor != highlightColorCache)
        {
            highlightColorCache = highlightColor;
            Highlight(highlightColor);
        }
    }
}
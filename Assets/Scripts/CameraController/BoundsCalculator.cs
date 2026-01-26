using UnityEngine;

public class BoundsCalculator : MonoBehaviour, IBoundsGetter
{
    public Bounds bounds { get; private set; } = new Bounds();
    [SerializeField] private bool buttonToRecalculateBounds = false;
    private bool buttonToRecalculateBoundsCache = false;

    void OnValidate()
    {
        if (buttonToRecalculateBounds != buttonToRecalculateBoundsCache)
        {
            buttonToRecalculateBoundsCache = buttonToRecalculateBounds;
            RecalculateBounds();
        }
    }

    void Awake()
    {
        RecalculateBounds();
    }

    private void RecalculateBounds()
    {
        Bounds newBounds = new Bounds();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            newBounds.Encapsulate(renderer.bounds);
        }
        this.bounds = newBounds;
    }
}

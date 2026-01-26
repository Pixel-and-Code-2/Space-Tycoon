using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BoundsIntroducer : MonoBehaviour, IBoundsGetter
{
    private BoxCollider boxCollider;
    public Bounds bounds { get => boxCollider.bounds; }

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }
}

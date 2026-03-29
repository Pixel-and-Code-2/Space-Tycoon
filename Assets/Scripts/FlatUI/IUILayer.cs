using UnityEngine;

public abstract class IUILayer : MonoBehaviour
{
    [SerializeField]
    public virtual bool isBackgroundVisible => true;
    public virtual void OnBackgroundClick() { }
    public virtual void Initialize(string config) { }
    public virtual bool isStoppingGame => true;
}
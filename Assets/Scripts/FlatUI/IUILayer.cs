using UnityEngine;

public abstract class IUILayer : MonoBehaviour
{
    [SerializeField]
    public bool isBackgroundVisible = true;
    public virtual void OnBackgroundClick() { }
    public virtual void Initialize(string config) { }
}
using UnityEngine;

public class IScriptForClickable : MonoBehaviour
{
    public virtual void OnSelect() { }
    public virtual void OnStart() { }
    public virtual void OnDeselect() { }
    public virtual void OnComplete() { }
    public virtual void OnCancel() { }
    public virtual void OnProgress(float newProgress) { }
}
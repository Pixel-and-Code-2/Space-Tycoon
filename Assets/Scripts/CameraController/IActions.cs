using UnityEngine;

public abstract class IActions : MonoBehaviour
{
    public abstract float GetLookReleaseValue();
    public abstract float GetZoomValue();
    public abstract Vector2 GetMoveValue();
    public abstract Vector2 GetLookValue();
    public abstract float GetRotPositive();
    public abstract float GetRotNegative();
}
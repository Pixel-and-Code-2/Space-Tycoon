using UnityEngine;

class AutoCameraControlActions : IActions
{
    public override float GetLookReleaseValue()
    {
        return 0f;
    }
    public override float GetZoomValue()
    {
        return 0f;
    }
    public override Vector2 GetMoveValue()
    {
        float moveX = Random.Range(-1f, 1f);
        float moveZ = Random.Range(-1f, 1f);
        return new Vector2(moveX, moveZ);
    }
    public override Vector2 GetLookValue()
    {
        return Vector2.zero;
    }
    public override float GetRotPositive()
    {
        return 0f;
    }
    public override float GetRotNegative()
    {
        return 0f;
    }
}
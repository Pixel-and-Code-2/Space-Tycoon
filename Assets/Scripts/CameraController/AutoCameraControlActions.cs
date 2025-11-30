using UnityEngine;

class AutoCameraControlActions : MonoBehaviour, IActions
{
    public float GetLookReleaseValue()
    {
        return 0f;
    }
    public float GetZoomValue()
    {
        return 0f;
    }
    public Vector2 GetMoveValue()
    {
        float moveX = Random.Range(-1f, 1f);
        float moveZ = Random.Range(-1f, 1f);
        return new Vector2(moveX, moveZ);
    }

    public Vector2 GetLookValue()
    {
        return Vector2.zero;
    }
}
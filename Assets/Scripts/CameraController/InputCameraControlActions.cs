using UnityEngine;
using UnityEngine.InputSystem;

public class InputCameraControlActions : MonoBehaviour, IActions
{
    [SerializeField]
    private InputActionReference lookReleaseActionReference;
    [SerializeField]
    private InputActionReference zoomActionReference;
    [SerializeField]
    private InputActionReference moveActionReference;
    [SerializeField]
    private InputActionReference lookActionReference;

    void OnValidate()
    {
        lookReleaseActionReference.action.Enable();
        zoomActionReference.action.Enable();
        moveActionReference.action.Enable();
        lookActionReference.action.Enable();
    }
    public float GetLookReleaseValue()
    {
        return lookReleaseActionReference.action.ReadValue<float>();
    }
    public float GetZoomValue()
    {
        return zoomActionReference.action.ReadValue<Vector2>().y;
    }
    public Vector2 GetMoveValue()
    {
        Vector2 move = moveActionReference.action.ReadValue<Vector2>();
        Vector2 inversedMove = new Vector2(move.y, move.x);
        return inversedMove;
    }
    public Vector2 GetLookValue()
    {
        return lookActionReference.action.ReadValue<Vector2>();
    }
}
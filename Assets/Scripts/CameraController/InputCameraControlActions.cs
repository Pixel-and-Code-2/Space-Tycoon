using UnityEngine;
using UnityEngine.InputSystem;

public class InputCameraControlActions : IActions
{
    [SerializeField]
    private InputActionReference lookReleaseActionReference;
    [SerializeField]
    private InputActionReference zoomActionReference;
    [SerializeField]
    private InputActionReference moveActionReference;
    [SerializeField]
    private InputActionReference lookActionReference;
    [SerializeField]
    private InputActionReference rotPositive;
    [SerializeField]
    private InputActionReference rotNegative;

    void OnEnable()
    {
        if (lookReleaseActionReference != null) lookReleaseActionReference.action.Enable();
        if (zoomActionReference != null) zoomActionReference.action.Enable();
        if (moveActionReference != null) moveActionReference.action.Enable();
        if (lookActionReference != null) lookActionReference.action.Enable();
        if (rotPositive != null) rotPositive.action.Enable();
        if (rotNegative != null) rotNegative.action.Enable();
    }
    void OnDisable()
    {
        if (lookReleaseActionReference != null) lookReleaseActionReference.action.Disable();
        if (zoomActionReference != null) zoomActionReference.action.Disable();
        if (moveActionReference != null) moveActionReference.action.Disable();
        if (lookActionReference != null) lookActionReference.action.Disable();
        if (rotPositive != null) rotPositive.action.Disable();
        if (rotNegative != null) rotNegative.action.Disable();
    }
    public override float GetLookReleaseValue()
    {
        if (lookReleaseActionReference != null) return lookReleaseActionReference.action.ReadValue<float>();
        return 0f;
    }
    public override float GetZoomValue()
    {
        if (zoomActionReference != null) return zoomActionReference.action.ReadValue<Vector2>().y;
        return 0f;
    }
    public override Vector2 GetMoveValue()
    {
        Vector2 move = Vector2.zero;
        if (moveActionReference != null) move = moveActionReference.action.ReadValue<Vector2>();
        Vector2 inversedMove = new Vector2(move.y, move.x);
        return inversedMove;
    }
    public override Vector2 GetLookValue()
    {
        Vector2 look = Vector2.zero;
        if (lookActionReference != null) look = lookActionReference.action.ReadValue<Vector2>();
        return look;
    }
    public override float GetRotPositive()
    {
        if (rotPositive != null) return rotPositive.action.ReadValue<float>();
        return 0f;
    }
    public override float GetRotNegative()
    {
        if (rotNegative != null) return rotNegative.action.ReadValue<float>();
        return 0f;
    }
}
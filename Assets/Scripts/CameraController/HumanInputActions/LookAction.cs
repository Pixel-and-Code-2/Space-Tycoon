using UnityEngine;
using UnityEngine.InputSystem;

public class LookAction : MonoBehaviour, ILookAction
{
    [SerializeField]
    private InputActionReference lookActionReference;
    void OnValidate()
    {
        lookActionReference.action.Enable();
    }
    public Vector2 LookValue
    {
        get { return lookActionReference.action.ReadValue<Vector2>(); }
    }
}
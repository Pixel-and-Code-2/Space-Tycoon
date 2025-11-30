using UnityEngine;
using UnityEngine.InputSystem;

public class MoveAction : MonoBehaviour, IMoveAction
{
    [SerializeField]
    private InputActionReference moveActionReference;
    void OnValidate()
    {
        moveActionReference.action.Enable();
    }
    public Vector2 MoveValue
    {
        get { return moveActionReference.action.ReadValue<Vector2>(); }
    }
}
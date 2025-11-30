using UnityEngine;
using UnityEngine.InputSystem;

public class LookReleaseAction : MonoBehaviour, ILookReleaseAction
{
    [SerializeField]
    private InputActionReference lookReleaserActionReference;
    void OnValidate()
    {
        lookReleaserActionReference.action.Enable();
    }
    public float LookReleaseValue
    {
        get { return lookReleaserActionReference.action.ReadValue<float>(); }
    }
}

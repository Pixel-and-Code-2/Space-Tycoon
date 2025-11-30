using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomAction : MonoBehaviour, IZoomAction
{
    [SerializeField]
    private InputActionReference zoomActionReference;
    void OnValidate()
    {
        zoomActionReference.action.Enable();
    }
    public float ZoomValue
    {
        get { return zoomActionReference.action.ReadValue<Vector2>().y; }
    }
}
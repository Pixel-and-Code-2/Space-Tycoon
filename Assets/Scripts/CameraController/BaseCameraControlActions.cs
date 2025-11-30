using UnityEngine;
interface ILookReleaseAction
{
    float LookReleaseValue { get; }
}

interface IZoomAction
{
    float ZoomValue { get; }
}

interface IMoveAction
{
    Vector2 MoveValue { get; }
}

interface ILookAction
{
    Vector2 LookValue { get; }
}

[RequireComponent(typeof(ILookReleaseAction))]
[RequireComponent(typeof(IZoomAction))]
[RequireComponent(typeof(IMoveAction))]
[RequireComponent(typeof(ILookAction))]
public class BaseCameraControlActions : MonoBehaviour
{
    [SerializeField]
    protected MonoBehaviour lookReleaseActionImplementation;
    private ILookReleaseAction lookReleaseAction;
    [SerializeField]
    protected MonoBehaviour zoomActionImplementation;
    private IZoomAction zoomAction;
    [SerializeField]
    protected MonoBehaviour moveActionImplementation;
    private IMoveAction moveAction;
    [SerializeField]
    protected MonoBehaviour lookActionImplementation;
    private ILookAction lookAction;

    void OnValidate()
    {

        if (lookReleaseActionImplementation != null)
        {
            if (lookReleaseActionImplementation is ILookReleaseAction)
            {
                lookReleaseAction = lookReleaseActionImplementation as ILookReleaseAction;
            }
            else
            {
                Debug.LogError("LookReleaseActionImplementation is not a ILookReleaseAction");
                lookReleaseAction = null;
            }
        }
        else
        {
            lookReleaseActionImplementation = gameObject.GetComponent<LookReleaseAction>();
        }
        if (zoomActionImplementation != null)
        {
            if (zoomActionImplementation is IZoomAction)
            {
                zoomAction = zoomActionImplementation as IZoomAction;
            }
            else
            {
                Debug.LogError("ZoomActionImplementation is not a IZoomAction");
                zoomAction = null;
            }
        }
        else
        {
            zoomActionImplementation = gameObject.GetComponent<ZoomAction>();
        }
        if (moveActionImplementation != null)
        {
            if (moveActionImplementation is IMoveAction)
            {
                moveAction = moveActionImplementation as IMoveAction;
            }
            else
            {
                Debug.LogError("MoveActionImplementation is not a IMoveAction");
                moveAction = null;
            }
        }
        else
        {
            moveActionImplementation = gameObject.GetComponent<MoveAction>();
        }
        if (lookActionImplementation != null)
        {
            if (lookActionImplementation is ILookAction)
            {
                lookAction = lookActionImplementation as ILookAction;
            }
            else
            {
                Debug.LogError("LookActionImplementation is not a ILookAction");
                lookAction = null;
            }
        }
        else
        {
            lookActionImplementation = gameObject.GetComponent<LookAction>();
        }
    }
    public float GetLookReleaseValue()
    {
        return lookReleaseAction.LookReleaseValue;
    }
    public float GetZoomValue()
    {
        return zoomAction.ZoomValue;
    }
    public Vector2 GetMoveValue()
    {
        return moveAction.MoveValue;
    }
    public Vector2 GetLookValue()
    {
        return lookAction.LookValue;
    }
}
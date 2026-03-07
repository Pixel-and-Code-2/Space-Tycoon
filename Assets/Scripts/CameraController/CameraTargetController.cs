using UnityEngine;

[RequireComponent(typeof(CameraController))]
public class CameraTargetController : MonoBehaviour
{
    [SerializeField] private IActions defaultActions;
    [SerializeField] private IActions onPawnActions;
    [SerializeField] private ILookTarget defaultLookTarget;
    private CameraController cameraController;
    private ILookTarget currentLookTarget;
    void Awake()
    {
        cameraController = GetComponent<CameraController>();
    }

    void Start()
    {
        cameraController.SetLookTarget(defaultLookTarget);
        cameraController.cameraControlActions = defaultActions;
        defaultActions.enabled = true;
    }

    void Update()
    {
        if (ClickableItemsController.Instance.currentSelectedItem != null)
        {
            if (currentLookTarget != ClickableItemsController.Instance.currentSelectedItem)
            {
                currentLookTarget = ClickableItemsController.Instance.currentSelectedItem;
                SetPawnTarget(currentLookTarget);
            }
        }
        else if (currentLookTarget != PawnController.Instance.currentSelectedPawn)
        {
            currentLookTarget = PawnController.Instance.currentSelectedPawn;
            if (currentLookTarget == null) UnsetPawnTarget();
            else SetPawnTarget(currentLookTarget);
        }
    }

    public void SetPawnTarget(ILookTarget lookTarget)
    {
        cameraController.SetLookTarget(lookTarget);
        cameraController.cameraControlActions = onPawnActions;
        defaultActions.enabled = false;
        onPawnActions.enabled = true;
    }
    public void UnsetPawnTarget()
    {
        cameraController.SetLookTarget(defaultLookTarget);
        cameraController.cameraControlActions = defaultActions;
        onPawnActions.enabled = false;
        defaultActions.enabled = true;
    }
}
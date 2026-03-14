using UnityEngine;

[RequireComponent(typeof(CameraController))]
public class CameraTargetController : MonoBehaviour
{
    [SerializeField] private IActions defaultActions;
    [SerializeField] private IActions onPawnActions;
    [SerializeField] private ILookTarget defaultLookTarget;
    [SerializeField] private bool listenOnlyPlayerControls = true;
    private CameraController cameraController;
    private ILookTarget currentLookTarget;
    private bool isLockedOnTarget = false;
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
        if (listenOnlyPlayerControls)
        {
            if (PawnController.Instance.currentSelector != PawnController.Instance.playerSelectorBrain) return;
        }
        // if (ClickableItemsController.Instance.currentSelectedItem != null)
        // {
        //     if (currentLookTarget != ClickableItemsController.Instance.currentSelectedItem)
        //     {
        //         currentLookTarget = ClickableItemsController.Instance.currentSelectedItem;
        //         SetPawnTarget(currentLookTarget);
        //     }
        // }
        // else
        if (currentLookTarget != PawnController.Instance.currentSelectedPawn)
        {
            if (PawnController.Instance.currentSelectedPawn == null) UnsetPawnTarget();
            else SetPawnTarget(PawnController.Instance.currentSelectedPawn);
        }
        if (cameraController.cameraControlActions.GetMoveValue() != Vector2.zero)
        {
            if (isLockedOnTarget)
            {
                UnlockTarget();
            }
            if (PawnController.Instance.currentSelectorWithUICached != null)
            {
                PawnController.Instance.currentSelectorWithUICached.SetUICacheAsDirty();
            }
        }
    }

    public void SetPawnTarget(ILookTarget lookTarget)
    {
        currentLookTarget = lookTarget;
        cameraController.SetLookTarget(lookTarget);
        cameraController.cameraControlActions = onPawnActions;
        defaultActions.enabled = false;
        onPawnActions.enabled = true;
        isLockedOnTarget = true;
    }
    public void UnsetPawnTarget()
    {
        UnlockTarget();
        currentLookTarget = null;
        cameraController.cameraControlActions = defaultActions;
        onPawnActions.enabled = false;
        defaultActions.enabled = true;
    }
    public void UnlockTarget()
    {
        isLockedOnTarget = false;
        defaultLookTarget.GetTransform().position = currentLookTarget.GetTransform().position;
        cameraController.SetLookTarget(defaultLookTarget);
    }
}
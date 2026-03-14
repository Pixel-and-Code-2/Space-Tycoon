using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CameraController))]
public class CameraTargetController : MonoBehaviour
{
    [SerializeField] private IActions defaultActions;
    [SerializeField] private IActions onPawnActions;
    [SerializeField] private ILookTarget defaultLookTarget;
    [SerializeField] private bool listenOnlyPlayerControls = true;
    [SerializeField] private InputActionReference lockTargetAction;
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

    void OnEnable()
    {
        if (lockTargetAction != null)
            lockTargetAction.action.Enable();
    }

    void OnDisable()
    {
        if (lockTargetAction != null)
            lockTargetAction.action.Disable();
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
        if (lockTargetAction != null && !isLockedOnTarget && currentLookTarget != null && lockTargetAction.action.ReadValue<float>() == 1.0f)
        {
            LockTarget();
        }
    }

    private void SetPawnTarget(ILookTarget lookTarget)
    {
        currentLookTarget = lookTarget;
        cameraController.cameraControlActions = onPawnActions;
        defaultActions.enabled = false;
        onPawnActions.enabled = true;
        LockTarget();
    }
    private void UnsetPawnTarget()
    {
        UnlockTarget();
        currentLookTarget = null;
        cameraController.cameraControlActions = defaultActions;
        onPawnActions.enabled = false;
        defaultActions.enabled = true;
    }
    private void UnlockTarget()
    {
        isLockedOnTarget = false;
        defaultLookTarget.GetTransform().position = currentLookTarget.GetTransform().position;
        cameraController.SetLookTarget(defaultLookTarget);
    }
    private void LockTarget()
    {
        isLockedOnTarget = true;
        if (cameraController != null)
            cameraController.SetLookTarget(currentLookTarget);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CameraController))]
public class CameraTargetController : MonoBehaviour
{
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
            if (PawnController.Instance.currentSelector != PawnController.Instance.playerSelectorBrain)
            {
                UnsetPawnTarget();
                return;
            }
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
            if (currentLookTarget != null) UnsetPawnTarget();
            if (PawnController.Instance.currentSelectedPawn != null) SetPawnTarget(PawnController.Instance.currentSelectedPawn);
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
        LockTarget();
    }
    private void UnsetPawnTarget()
    {
        UnlockTarget();
        currentLookTarget = null;
    }
    private void UnlockTarget()
    {
        if (!isLockedOnTarget) return;
        isLockedOnTarget = false;
        defaultLookTarget.GetTransform().position = currentLookTarget.GetTransform().position;
        cameraController.SetLookTarget(defaultLookTarget);
    }
    private void LockTarget()
    {
        if (isLockedOnTarget) return;
        isLockedOnTarget = true;
        if (cameraController != null)
            cameraController.SetLookTarget(currentLookTarget);
    }
}
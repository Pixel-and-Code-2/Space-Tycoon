using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CameraController))]
public class CameraTargetController : MonoBehaviour
{
    public static CameraTargetController Instance { get; private set; }
    [SerializeField] private ILookTarget defaultLookTarget;
    [SerializeField] private bool listenOnlyPlayerControls = true;
    [SerializeField] private InputActionReference lockTargetAction;
    private CameraController cameraController;
    private ILookTarget currentLookTarget;
    private bool isLockedOnTarget = false;
    [SerializeField]
    private bool lockOnSelect = true;
    [SerializeField]
    private bool lockOnForceSelect = true;
    private bool isForced = false;
    private bool manualFocus;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of CameraTargetController found, destroying the extra one");
        }
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
        if (!manualFocus)
            CheckTarget();
        if (cameraController.cameraControlActions.GetMoveValue() != Vector2.zero)
        {
            manualFocus = false;
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
    public void ForceLockTarget()
    {
        isForced = true;
    }

    public void FocusOnLookTarget(ILookTarget lookTarget)
    {
        if (lookTarget == null) return;
        if (isLockedOnTarget)
            UnlockTarget();
        manualFocus = true;
        currentLookTarget = lookTarget;
        LockTarget();
    }

    public void CheckTarget()
    {
        if (currentLookTarget != PawnController.Instance.currentSelectedPawn)
        {
            if (currentLookTarget != null) UnsetPawnTarget();
            if (PawnController.Instance.currentSelectedPawn != null) SetPawnTarget(PawnController.Instance.currentSelectedPawn);
        }
    }

    private void SetPawnTarget(ILookTarget lookTarget)
    {
        manualFocus = false;
        currentLookTarget = lookTarget;
        if (lockOnSelect || (isForced && lockOnForceSelect))
        {
            isForced = false;
            LockTarget();
        }
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
using UnityEngine;

// ToDo: in cinemachine orbital follow exists sphere / three rings types, we are using second one, but the first one is better

[RequireComponent(typeof(IActions))]
public class CameraController : CameraSettings
{
    [Header("Input Configuration")]
    public IActions cameraControlActions;
    [SerializeField] private Vector2 rotationSpeeds = Vector2.one;
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private float zoomSensitivity = 1f;
    [SerializeField] private float minCameraRadiusCoef = 0.2f;
    private float minCameraRadiusCoefCache = 0.2f;
    private const float maxCameraRaduisCoef = 1f;
    [SerializeField] private AnimationCurve zoomChangeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float discreteRotationStep = 90f;

    new void OnValidate()
    {
        base.OnValidate();
        if (minCameraRadiusCoef != minCameraRadiusCoefCache)
        {
            minCameraRadiusCoefCache = minCameraRadiusCoef;
            minCameraRadiusCoef = Mathf.Clamp(minCameraRadiusCoef, 0.01f, maxCameraRaduisCoef);
            orbitalFollow.RadialAxis.Range[0] = minCameraRadiusCoef;
        }
    }

    new void Awake()
    {
        orbitalFollow.RadialAxis.Range[0] = minCameraRadiusCoef;
        orbitalFollow.RadialAxis.Range[1] = maxCameraRaduisCoef;
        base.Awake();
    }

    void Update()
    {
        HandleRotationInput();
        HandleDiscreteRotationInput();
        HandleMoveInput();
        HandleZoomInput();
    }

    private bool isRotPosHandled = false;
    private bool isRotNegHandled = false;
    void HandleDiscreteRotationInput()
    {
        if (cameraControlActions == null) return;
        if (cameraControlActions.GetRotPositive() != 0f)
        {
            if (isRotPosHandled) return;
            isRotPosHandled = true;
            float rotation = cameraControlActions.GetRotPositive();
            float newValue = orbitalFollow.HorizontalAxis.Value - rotation * discreteRotationStep;
            if (!orbitalFollow.HorizontalAxis.Wrap)
            {
                newValue = Mathf.Clamp(newValue, orbitalFollow.HorizontalAxis.Range[0], orbitalFollow.HorizontalAxis.Range[1]);
            }
            orbitalFollow.HorizontalAxis.Center = newValue;
            orbitalFollow.HorizontalAxis.TriggerRecentering();
        }
        else
        {
            isRotPosHandled = false;
        }
        if (cameraControlActions.GetRotNegative() != 0f)
        {
            if (isRotNegHandled) return;
            isRotNegHandled = true;
            float rotation = cameraControlActions.GetRotNegative();
            float newValue = orbitalFollow.HorizontalAxis.Value + rotation * discreteRotationStep;
            if (!orbitalFollow.HorizontalAxis.Wrap)
            {
                newValue = Mathf.Clamp(newValue, orbitalFollow.HorizontalAxis.Range[0], orbitalFollow.HorizontalAxis.Range[1]);
            }
            orbitalFollow.HorizontalAxis.Center = newValue;
            orbitalFollow.HorizontalAxis.TriggerRecentering();
        }
        else
        {
            isRotNegHandled = false;
        }
    }
    void HandleRotationInput()
    {
        if (cameraControlActions == null) return;
        if (cameraControlActions.GetLookReleaseValue() != 0f)
        {
            Vector2 mouseMove = cameraControlActions.GetLookValue();
            float currentX = orbitalFollow.HorizontalAxis.Value;
            float mouseX = mouseMove.x * rotationSpeeds.x;
            float mouseY = mouseMove.y * rotationSpeeds.y;
            float clampedMouseX = mouseX;
            if (!orbitalFollow.HorizontalAxis.Wrap)
            {
                clampedMouseX = Mathf.Clamp(currentX + mouseX, orbitalFollow.HorizontalAxis.Range[0], orbitalFollow.HorizontalAxis.Range[1]) - currentX;
            }
            orbitalFollow.HorizontalAxis.Value += clampedMouseX;
            float clampedMouseY = Mathf.Clamp(orbitalFollow.VerticalAxis.Value + mouseY, orbitalFollow.VerticalAxis.Range[0], orbitalFollow.VerticalAxis.Range[1]) - orbitalFollow.VerticalAxis.Value;
            orbitalFollow.VerticalAxis.Value += clampedMouseY;
        }
    }

    void HandleZoomInput()
    {
        if (cameraControlActions == null) return;
        float scrollDelta = cameraControlActions.GetZoomValue();

        if (scrollDelta != 0f)
        {
            float fovValuesRange = maxCameraRaduisCoef - minCameraRadiusCoef;
            float currValue = orbitalFollow.RadialAxis.Value - minCameraRadiusCoef;
            float inPercent = Mathf.Clamp(currValue, 0, fovValuesRange) / fovValuesRange;
            float valueWithCurve = Mathf.Clamp(zoomChangeCurve.Evaluate(inPercent), 0.01f, 1f);
            float change = scrollDelta * zoomSensitivity * valueWithCurve;
            orbitalFollow.RadialAxis.Value = Mathf.Clamp(orbitalFollow.RadialAxis.Value - change, minCameraRadiusCoef, maxCameraRaduisCoef);
        }
    }

    void HandleMoveInput()
    {
        if (cameraControlActions == null) return;
        if (!lookTarget.isMovable) return;
        Vector2 move = cameraControlActions.GetMoveValue();
        float moveX = move.x;
        float moveZ = move.y;

        if (moveX == 0 && moveZ == 0) return;
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();
        Vector3 right = Camera.main.transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 movement = panSpeed * Time.deltaTime *
                          (forward * moveX +
                           right * moveZ);

        Vector3 proposedPosition = lookTarget.GetTransform().position + movement;

        proposedPosition.x = Mathf.Clamp(proposedPosition.x, boundsGetter.bounds.min.x, boundsGetter.bounds.max.x);
        proposedPosition.z = Mathf.Clamp(proposedPosition.z, boundsGetter.bounds.min.z, boundsGetter.bounds.max.z);
        proposedPosition.y = lookTarget.GetTransform().position.y;

        lookTarget.GetTransform().position = proposedPosition;
    }
}

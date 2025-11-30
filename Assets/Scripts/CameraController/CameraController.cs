using UnityEngine;

[RequireComponent(typeof(IActions))]
public class CameraController : CameraSettings
{
    [Header("Input Configuration")]
    [SerializeField] private MonoBehaviour cameraControlActionsRef;
    private IActions cameraControlActions;
    [SerializeField] private Vector2 rotationSpeeds = Vector2.one;
    [SerializeField] private float panSpeed = 1f;

    [SerializeField] private float zoomSensitivity = 1f;
    [SerializeField] private float minCameraRadiusCoef = 0.2f;
    private float minCameraRadiusCoefCache = 0.2f;
    private const float maxCameraRaduisCoef = 1f;
    [SerializeField] private AnimationCurve zoomChangeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
        cameraControlActions = cameraControlActionsRef as IActions;
    }

    void Update()
    {
        HandleRotationInput();
        HandleMoveInput();
        HandleZoomInput();
    }

    void HandleRotationInput()
    {
        if (cameraControlActions.GetLookReleaseValue() != 0f)
        {
            Vector2 mouseMove = cameraControlActions.GetLookValue();
            float mouseX = mouseMove.x * rotationSpeeds.x;
            float mouseY = mouseMove.y * rotationSpeeds.y;
            orbitalFollow.HorizontalAxis.Value += mouseX;
            orbitalFollow.HorizontalAxis.Value = Mathf.Clamp(orbitalFollow.HorizontalAxis.Value, orbitalFollow.HorizontalAxis.Range[0], orbitalFollow.HorizontalAxis.Range[1]);

            float newY = orbitalFollow.VerticalAxis.Value + mouseY;
            float maxY = orbitalFollow.VerticalAxis.Range[1];
            float minY = orbitalFollow.VerticalAxis.Range[0];
            newY = Mathf.Clamp(newY, minY, maxY);
            orbitalFollow.VerticalAxis.Value = newY;

            lookTarget.Rotate(Vector3.up, mouseX, Space.World);
        }
    }

    void HandleZoomInput()
    {
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
        Vector2 move = cameraControlActions.GetMoveValue();
        float moveX = move.y;
        float moveZ = move.x;

        if (moveX == 0 && moveZ == 0) return;

        Vector3 movement = panSpeed * Time.deltaTime *
                          (lookTarget.transform.forward * moveX +
                           lookTarget.transform.right * moveZ);

        Vector3 proposedPosition = lookTarget.position + movement;

        Bounds bounds = boundsCollider.bounds;
        proposedPosition.x = Mathf.Clamp(proposedPosition.x, bounds.min.x, bounds.max.x);
        proposedPosition.z = Mathf.Clamp(proposedPosition.z, bounds.min.z, bounds.max.z);
        proposedPosition.y = lookTarget.position.y;

        lookTarget.position = proposedPosition;
    }
}

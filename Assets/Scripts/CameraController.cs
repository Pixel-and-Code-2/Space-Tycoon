using UnityEngine;
using Unity.Cinemachine;
using System;// for Math
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera settings")]
    [SerializeField] private float maxCameraRadius = 10f;
    private float maxCameraRadiusCache = 10f;
    [SerializeField] private float cameraTopOffset = 5f;
    private float cameraTopOffsetCache = 5f;
    [SerializeField] private float cameraBottomOffset = -5f;
    private float cameraBottomOffsetCache = 5f;
    [SerializeField] private float cameraInitialDegreeHeight = 15f;

    [Header("Input Configuration")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference lookReleaserAction;
    [SerializeField] private Vector2 rotationSpeeds = Vector2.one;
    [SerializeField] private InputActionReference walkAction;
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private InputActionReference zoomAction;

    [SerializeField] private float zoomSensitivity = 1f;
    [SerializeField] private float minCameraRaduisCoef = 0.2f;
    private float minCameraRaduisCoefCache = 0.2f;
    private const float maxCameraRaduisCoef = 1f;
    [SerializeField] private AnimationCurve zoomChangeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    [Header("References")]
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private Collider boundsCollider;
    [SerializeField] private Transform lookTarget;

    void OnValidate()
    {
        if (maxCameraRadius != maxCameraRadiusCache
         || cameraTopOffset != cameraTopOffsetCache
         || cameraBottomOffset != cameraBottomOffsetCache)
        {
            maxCameraRadiusCache = maxCameraRadius;
            cameraTopOffset = Math.Clamp(cameraTopOffset, 0, 0.95f * maxCameraRadius);
            cameraTopOffsetCache = cameraTopOffset;
            cameraBottomOffset = Math.Clamp(cameraBottomOffset, -0.95f * maxCameraRadius, cameraTopOffset);
            cameraBottomOffsetCache = cameraBottomOffset;
            ApplyNewRadius();
        }

        if (cameraInitialDegreeHeight != orbitalFollow.VerticalAxis.Value)
        {
            cameraInitialDegreeHeight = Math.Clamp(cameraInitialDegreeHeight, orbitalFollow.VerticalAxis.Range[0], orbitalFollow.VerticalAxis.Range[1]);
            orbitalFollow.VerticalAxis.Value = cameraInitialDegreeHeight;
        }

        if (minCameraRaduisCoef != minCameraRaduisCoefCache)
        {
            minCameraRaduisCoefCache = minCameraRaduisCoef;
            minCameraRaduisCoef = Math.Clamp(minCameraRaduisCoef, 0.01f, maxCameraRaduisCoef);
            orbitalFollow.RadialAxis.Range[0] = minCameraRaduisCoef;
        }
    }

    void ApplyNewRadius()
    {
        orbitalFollow.Orbits.Center.Radius = maxCameraRadius;
        orbitalFollow.Orbits.Top.Height = Math.Clamp(cameraTopOffset, 0, maxCameraRadius);
        orbitalFollow.Orbits.Bottom.Height = Math.Clamp(cameraBottomOffset, -maxCameraRadius, 0);

        float newTopRadius = (float)Math.Pow(maxCameraRadius * maxCameraRadius - cameraTopOffset * cameraTopOffset, 0.5);
        orbitalFollow.Orbits.Top.Radius = newTopRadius;
        float newBottomRadius = (float)Math.Pow(maxCameraRadius * maxCameraRadius - cameraBottomOffset * cameraBottomOffset, 0.5);
        orbitalFollow.Orbits.Bottom.Radius = newBottomRadius;
    }

    void Awake()
    {
        orbitalFollow.Orbits.Center.Height = 0;
        orbitalFollow.Orbits.Top.Height = cameraTopOffset;
        orbitalFollow.Orbits.Bottom.Height = cameraBottomOffset;
        orbitalFollow.RadialAxis.Range[0] = minCameraRaduisCoef;
        orbitalFollow.RadialAxis.Range[1] = maxCameraRaduisCoef;
        ApplyNewRadius();
        zoomAction.action.Enable();
        walkAction.action.Enable();
        lookAction.action.Enable();
        lookReleaserAction.action.Enable();
    }

    void Update()
    {
        HandleRotationInput();
        HandleMoveInput();
        HandleZoomInput();
    }

    void HandleRotationInput()
    {
        if (lookReleaserAction.action.ReadValue<float>() != 0f)
        {
            Vector2 mouseMove = lookAction.action.ReadValue<Vector2>();
            float mouseX = mouseMove.x * rotationSpeeds.x;
            float mouseY = mouseMove.y * rotationSpeeds.y;
            orbitalFollow.HorizontalAxis.Value += mouseX;

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
        float scrollDelta = zoomAction.action.ReadValue<Vector2>().y;

        if (scrollDelta != 0f)
        {
            float fovValuesRange = maxCameraRaduisCoef - minCameraRaduisCoef;
            float currValue = orbitalFollow.RadialAxis.Value - minCameraRaduisCoef;
            float inPercent = Mathf.Clamp(currValue, 0, fovValuesRange) / fovValuesRange;
            float valueWithCurve = Math.Clamp(zoomChangeCurve.Evaluate(inPercent), 0.01f, 1f);
            float change = scrollDelta * zoomSensitivity * valueWithCurve;
            orbitalFollow.RadialAxis.Value = Math.Clamp(orbitalFollow.RadialAxis.Value - change, minCameraRaduisCoef, maxCameraRaduisCoef);
        }
    }

    void HandleMoveInput()
    {
        Vector2 move = walkAction.action.ReadValue<Vector2>();
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



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boundsCollider.bounds.center, boundsCollider.bounds.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxCameraRadius);

        Vector3 center = transform.position + new Vector3(0f, cameraTopOffset, 0f);
        float radius = orbitalFollow.Orbits.Top.Radius;

        Vector3 startX = center + new Vector3(-radius, 0f, 0f);
        Vector3 endX = center + new Vector3(radius, 0f, 0f);

        Gizmos.DrawLine(startX, endX);

        center = transform.position + new Vector3(0f, cameraBottomOffset, 0f);
        radius = orbitalFollow.Orbits.Bottom.Radius;

        startX = center + new Vector3(0f, 0f, -radius);
        endX = center + new Vector3(0f, 0f, radius);

        Gizmos.DrawLine(startX, endX);
    }
}

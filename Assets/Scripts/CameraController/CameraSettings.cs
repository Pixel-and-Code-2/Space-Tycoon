using Unity.Cinemachine;
using UnityEngine;

public abstract class CameraSettings : MonoBehaviour
{
    [Header("Camera settings")]
    [SerializeField] private float maxCameraRadius = 10f;
    private float maxCameraRadiusCache = 10f;
    [SerializeField] private float cameraTopOffset = 5f;
    private float cameraTopOffsetCache = 5f;
    [SerializeField] private float cameraBottomOffset = -5f;
    private float cameraBottomOffsetCache = -5f;
    [SerializeField] private float cameraInitialDegreeHeight = 15f;
    [SerializeField] private float cameraInitialHorizontalDegree = 15f;
    [SerializeField, Range(0f, 180f)] private float cameraHorizontalDegreeLimitation = 15f;

    [Header("References")]
    [SerializeField] public CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] public CinemachineCamera virtualCamera;
    [SerializeField] public MonoBehaviour boundsGetterRef;
    public IBoundsGetter boundsGetter { get; private set; } = null;
    [SerializeField] protected ILookTarget lookTarget;


    public void SetLookTarget(ILookTarget lookTarget)
    {
        this.lookTarget = lookTarget;
        virtualCamera.Follow = lookTarget.GetTransform();
    }

    public IBoundsGetter GetBoundsGetter()
    {
        return boundsGetter;
    }

    protected void OnValidate()
    {
        if (maxCameraRadius != maxCameraRadiusCache
         || cameraTopOffset != cameraTopOffsetCache
         || cameraBottomOffset != cameraBottomOffsetCache)
        {
            maxCameraRadiusCache = maxCameraRadius;
            cameraTopOffset = Mathf.Clamp(cameraTopOffset, 0, 0.95f * maxCameraRadius);
            cameraTopOffsetCache = cameraTopOffset;
            cameraBottomOffset = Mathf.Clamp(cameraBottomOffset, -0.95f * maxCameraRadius, cameraTopOffset);
            cameraBottomOffsetCache = cameraBottomOffset;
            ApplyNewRadius();
        }

        if (cameraInitialDegreeHeight != orbitalFollow.VerticalAxis.Value)
        {
            cameraInitialDegreeHeight = Mathf.Clamp(cameraInitialDegreeHeight, orbitalFollow.VerticalAxis.Range[0], orbitalFollow.VerticalAxis.Range[1]);
            orbitalFollow.VerticalAxis.Value = cameraInitialDegreeHeight;
        }

        if (cameraInitialHorizontalDegree != orbitalFollow.HorizontalAxis.Value)
        {
            cameraInitialHorizontalDegree = Mathf.Clamp(cameraInitialHorizontalDegree, orbitalFollow.HorizontalAxis.Range[0], orbitalFollow.HorizontalAxis.Range[1]);
            orbitalFollow.HorizontalAxis.Value = cameraInitialHorizontalDegree;
            orbitalFollow.HorizontalAxis.Range[0] = cameraInitialHorizontalDegree - cameraHorizontalDegreeLimitation;
            orbitalFollow.HorizontalAxis.Range[1] = cameraInitialHorizontalDegree + cameraHorizontalDegreeLimitation;
            if (cameraInitialHorizontalDegree == 180f) orbitalFollow.HorizontalAxis.Wrap = true;
            else orbitalFollow.HorizontalAxis.Wrap = false;
        }

        if (2 * cameraHorizontalDegreeLimitation != orbitalFollow.HorizontalAxis.Range[1] - orbitalFollow.HorizontalAxis.Range[0])
        {
            orbitalFollow.HorizontalAxis.Range[0] = cameraInitialHorizontalDegree - cameraHorizontalDegreeLimitation;
            orbitalFollow.HorizontalAxis.Range[1] = cameraInitialHorizontalDegree + cameraHorizontalDegreeLimitation;
        }

        if (boundsGetterRef as IBoundsGetter != boundsGetter)
        {
            boundsGetter = boundsGetterRef as IBoundsGetter;
        }
        else if (boundsGetterRef == null)
        {
            boundsGetter = null;
        }
    }

    void ApplyNewRadius()
    {
        float topHeight = Mathf.Clamp(cameraTopOffset, 0, maxCameraRadius);
        float bottomHeight = Mathf.Clamp(cameraBottomOffset, -maxCameraRadius, topHeight);
        float centerHeight = (topHeight + bottomHeight) / 2;
        orbitalFollow.Orbits.Top.Height = topHeight;
        orbitalFollow.Orbits.Bottom.Height = bottomHeight;
        orbitalFollow.Orbits.Center.Height = centerHeight;
        orbitalFollow.HorizontalAxis.Value = cameraInitialHorizontalDegree;
        orbitalFollow.HorizontalAxis.Range[0] = cameraInitialHorizontalDegree - cameraHorizontalDegreeLimitation;
        orbitalFollow.HorizontalAxis.Range[1] = cameraInitialHorizontalDegree + cameraHorizontalDegreeLimitation;
        float topLimRad = Mathf.Asin(topHeight / maxCameraRadius);
        float topСameraLimitation = topLimRad * Mathf.Rad2Deg;
        float bottomLimRad = Mathf.Asin(bottomHeight / maxCameraRadius);
        float bottomCameraLimitation = bottomLimRad * Mathf.Rad2Deg;
        orbitalFollow.VerticalAxis.Range[0] = bottomCameraLimitation;
        orbitalFollow.VerticalAxis.Range[1] = topСameraLimitation;


        float newTopRadius = (float)Mathf.Pow(maxCameraRadius * maxCameraRadius - cameraTopOffset * cameraTopOffset, 0.5f);
        orbitalFollow.Orbits.Top.Radius = newTopRadius;
        float newBottomRadius = (float)Mathf.Pow(maxCameraRadius * maxCameraRadius - bottomHeight * bottomHeight, 0.5f);
        orbitalFollow.Orbits.Bottom.Radius = newBottomRadius;
        float newCenterRadius = (float)Mathf.Pow(maxCameraRadius * maxCameraRadius - centerHeight * centerHeight, 0.5f);
        orbitalFollow.Orbits.Center.Radius = newCenterRadius;
    }

    protected void Awake()
    {
        orbitalFollow.Orbits.Top.Height = cameraTopOffset;
        orbitalFollow.Orbits.Bottom.Height = cameraBottomOffset;
        ApplyNewRadius();
        OnValidate();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (boundsGetter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boundsGetter.bounds.center, boundsGetter.bounds.size);
        }

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
#endif
}
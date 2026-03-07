using UnityEngine;

public class PathDrawer : MonoBehaviour
{
    [SerializeField]
    private LineRenderer pathLineWalkable;
    [SerializeField]
    private LineRenderer pathLineOutOfRange;
    [SerializeField]
    private Transform pathStartObject;
    private Renderer pathStartObjectRenderer;
    [SerializeField]
    private Transform pathEndObject;
    private Renderer pathEndObjectRenderer;
    [SerializeField]
    private Material pathWalkableMaterial;
    [SerializeField]
    private Material pathOutOfRangeMaterial;
    public float availableDistance = 0f;
    [System.NonSerialized]
    private bool visible = true;
    [SerializeField]
    private bool updateStillnesOfEndPoints = false;

    void LateUpdate()
    {
        if (updateStillnesOfEndPoints)
        {
            if (pathLineWalkable.positionCount > 0)
            {
                if (pathEndObject != null) pathEndObject.transform.position = pathLineWalkable.GetPosition(pathLineWalkable.positionCount - 1);
                if (pathStartObject != null) pathStartObject.transform.position = pathLineWalkable.GetPosition(0);
            }
        }
    }

    public void SetPathPoints(Vector3[] pointsAvailable, Vector3[] pointsOutOfRange)
    {
        if (pointsAvailable == null && pointsOutOfRange == null)
        {
            SetVisible(false);
            return;
        }
        availableDistance = 0f;
        RecolorPathEnds(pointsAvailable, pointsOutOfRange);

        if (pointsAvailable != null)
        {
            pathLineWalkable.positionCount = pointsAvailable.Length;
            for (int i = 0; i < pointsAvailable.Length; i++)
            {
                pathLineWalkable.SetPosition(i, pointsAvailable[i]);
                if (i < pointsAvailable.Length - 1)
                {
                    availableDistance += Vector3.Distance(pointsAvailable[i], pointsAvailable[i + 1]);
                }
            }
        }

        if (pointsOutOfRange != null)
        {
            pathLineOutOfRange.positionCount = pointsOutOfRange.Length;
            for (int i = 0; i < pointsOutOfRange.Length; i++)
            {
                pathLineOutOfRange.SetPosition(i, pointsOutOfRange[i]);
                if (i < pointsOutOfRange.Length - 1)
                {
                    availableDistance += Vector3.Distance(pointsOutOfRange[i], pointsOutOfRange[i + 1]);
                }
            }
        }
    }

    private void RecolorPathEnds(Vector3[] pointsAvailable, Vector3[] pointsOutOfRange)
    {
        if (pointsAvailable != null)
        {
            if (pathStartObject != null) pathStartObject.transform.position = pointsAvailable[0];
            ColorStartTo(false);
        }
        else
        {
            if (pathStartObject != null) pathStartObject.transform.position = pointsOutOfRange[0];
            ColorStartTo(true);
        }

        if (pointsOutOfRange != null)
        {
            if (pathEndObject != null) pathEndObject.transform.position = pointsOutOfRange[^1];
            ColorEndTo(true);
        }
        else
        {
            if (pathEndObject != null) pathEndObject.transform.position = pointsAvailable[^1];
            ColorEndTo(false);
        }
    }

    protected virtual void ColorEndTo(bool isOutOfRange = true)
    {
        if (isOutOfRange)
        {
            if (pathEndObjectRenderer != null) pathEndObjectRenderer.material = pathOutOfRangeMaterial;
            pathLineOutOfRange.enabled = true;
        }
        else
        {
            if (pathEndObjectRenderer != null) pathEndObjectRenderer.material = pathWalkableMaterial;
            pathLineOutOfRange.enabled = false;
        }
    }

    protected virtual void ColorStartTo(bool isOutOfRange = false)
    {
        if (isOutOfRange)
        {
            if (pathStartObjectRenderer != null) pathStartObjectRenderer.material = pathOutOfRangeMaterial;
            pathLineOutOfRange.enabled = true;
            pathLineWalkable.enabled = false;
        }
        else
        {
            if (pathStartObjectRenderer != null) pathStartObjectRenderer.material = pathWalkableMaterial;
            pathLineWalkable.enabled = true;
        }
    }

    protected void Awake()
    {
        if (pathStartObject != null)
            pathStartObjectRenderer = pathStartObject.GetComponent<Renderer>();
        if (pathEndObject != null)
            pathEndObjectRenderer = pathEndObject.GetComponent<Renderer>();
        CheckVariables();
    }

    void Start()
    {
        SetVisible(false);
    }

    protected void OnValidate()
    {
        if (pathStartObjectRenderer == null && pathStartObject != null)
            pathStartObjectRenderer = pathStartObject.GetComponent<Renderer>();
        if (pathEndObjectRenderer == null && pathEndObject != null)
            pathEndObjectRenderer = pathEndObject.GetComponent<Renderer>();
        this.CheckVariables();
    }

    protected virtual void CheckVariables()
    {
        if (pathLineWalkable == null)
        {
            Debug.LogError("Path line walkable not found in " + name);
            return;
        }
        if (pathLineOutOfRange == null)
        {
            Debug.LogError("Path line out of range not found in " + name);
            return;
        }
    }

    public virtual void SetVisible(bool visible)
    {
        if (this.visible == visible) return;
        this.visible = visible;
        if (pathStartObject != null) pathStartObject.gameObject.SetActive(visible);
        if (pathEndObject != null) pathEndObject.gameObject.SetActive(visible);
        // if (pathStartObject != null) pathStartObject.GetComponent<Renderer>().enabled = visible;
        // if (pathEndObject != null) pathEndObject.GetComponent<Renderer>().enabled = visible;
        if (visible == false)
        {
            pathLineOutOfRange.enabled = false;
            pathLineWalkable.enabled = false;
        }
    }

    public bool GetVisible()
    {
        return visible;
    }
}
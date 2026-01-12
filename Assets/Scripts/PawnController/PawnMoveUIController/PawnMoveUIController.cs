using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PawnMoveUIController : MonoBehaviour
{
    [SerializeField]
    private LineRenderer pathLineWalkable;
    [SerializeField]
    private LineRenderer pathLineOutOfRange;
    [SerializeField]
    private GameObject canvasObject;
    private Canvas canvas;
    private TextMeshProUGUI distanceText;
    [SerializeField]
    private Transform pathStartObject;
    [SerializeField]
    private Transform pathEndObject;
    public Vector3[] pathPoints;
    private bool visible = false;

    [SerializeField]
    private Material pathWalkableMaterial;
    [SerializeField]
    private Material pathOutOfRangeMaterial;

    public void SetPathPoints(Vector3[] points, float availableDistance = -1f)
    {
        pathPoints = points;
        pathLineWalkable.positionCount = pathPoints.Length;
        for (int i = 0; i < pathPoints.Length; i++)
        {
            pathLineWalkable.SetPosition(i, pathPoints[i]);
        }
        if (pathStartObject != null)
            pathStartObject.position = pathPoints[0];
        if (pathEndObject != null)
            pathEndObject.position = pathPoints[^1];
        if (availableDistance > 0f)
        {
            canvas.transform.position = pathPoints[^1] + Vector3.up * 1f;
            distanceText.text = $"{availableDistance.ToString("F2")}m";

        }

    }

    void Awake()
    {
        canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found");
            return;
        }
        distanceText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        if (distanceText == null)
        {
            Debug.LogError("Distance text not found");
            return;
        }
        if (pathLineWalkable == null)
        {
            Debug.LogError("Path line walkable not found");
            return;
        }
        if (pathLineOutOfRange == null)
        {
            Debug.LogError("Path line out of range not found");
            return;
        }
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        this.visible = visible;
        canvas.enabled = visible;
        if (pathStartObject != null)
            pathStartObject.gameObject.SetActive(visible);
        if (pathEndObject != null)
            pathEndObject.gameObject.SetActive(visible);
        if (pathLineWalkable != null)
            pathLineWalkable.enabled = visible;
        if (pathLineOutOfRange != null)
            pathLineOutOfRange.enabled = visible;
    }

    public bool GetVisible()
    {
        return visible;
    }
}
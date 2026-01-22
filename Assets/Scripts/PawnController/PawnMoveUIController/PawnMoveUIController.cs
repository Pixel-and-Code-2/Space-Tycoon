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
    private RectTransform canvasRect;
    private TextMeshProUGUI distanceText;
    [SerializeField]
    private Vector2 distanceTextOffset = new Vector2(0f, 10f);
    [SerializeField]
    private Transform pathStartObject;
    private Renderer pathStartObjectRenderer;
    [SerializeField]
    private Transform pathEndObject;
    private Renderer pathEndObjectRenderer;
    private bool visible = false;

    [SerializeField]
    private Material pathWalkableMaterial;
    [SerializeField]
    private Material pathOutOfRangeMaterial;
    [SerializeField]
    private Color textColor = Color.green;
    [SerializeField]
    private Color outOfRangeTextColor = Color.red;

    public void SetPathPoints(Vector3[] pointsAvailable, Vector3[] pointsOutOfRange, Vector2 screenPoint)
    {
        if (pointsAvailable == null && pointsOutOfRange == null)
        {
            SetVisible(false);
            return;
        }
        float availableDistance = 0f;

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

        distanceText.text = $"{availableDistance.ToString("F1")}m";
        RectTransform textRect = distanceText.rectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.worldCamera,
            out Vector2 localPoint
        );
        textRect.anchoredPosition = localPoint + distanceTextOffset;
    }

    private void RecolorPathEnds(Vector3[] pointsAvailable, Vector3[] pointsOutOfRange)
    {
        if (pointsAvailable != null)
        {
            pathStartObject.transform.position = pointsAvailable[0];
            ColorStartTo(false);
        }
        else
        {
            pathStartObject.transform.position = pointsOutOfRange[0];
            ColorStartTo(true);
        }

        if (pointsOutOfRange != null)
        {
            pathEndObject.transform.position = pointsOutOfRange[^1];
            ColorEndTo(true);
        }
        else
        {
            pathEndObject.transform.position = pointsAvailable[^1];
            ColorEndTo(false);
        }
    }

    private void ColorEndTo(bool isOutOfRange = true)
    {
        if (isOutOfRange)
        {
            pathEndObjectRenderer.material = pathOutOfRangeMaterial;
            distanceText.color = outOfRangeTextColor;
            pathLineOutOfRange.enabled = true;
        }
        else
        {
            pathEndObjectRenderer.material = pathWalkableMaterial;
            distanceText.color = textColor;
            pathLineOutOfRange.enabled = false;
        }
    }

    private void ColorStartTo(bool isOutOfRange = false)
    {
        if (isOutOfRange)
        {
            pathStartObjectRenderer.material = pathOutOfRangeMaterial;
            pathLineOutOfRange.enabled = true;
            pathLineWalkable.enabled = false;
        }
        else
        {
            pathStartObjectRenderer.material = pathWalkableMaterial;
            pathLineWalkable.enabled = true;
        }
    }

    void Awake()
    {
        canvas = canvasObject.GetComponent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        distanceText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        if (pathStartObject != null)
            pathStartObjectRenderer = pathStartObject.GetComponent<Renderer>();
        if (pathEndObject != null)
            pathEndObjectRenderer = pathEndObject.GetComponent<Renderer>();
        CheckVariables();
        SetVisible(false);
    }

    void OnValidate()
    {
        if (
            (canvas == null && canvasObject != null) ||
            (canvasRect == null && canvas != null) ||
            (distanceText == null && canvas != null)
        )
        {
            canvas = canvasObject.GetComponent<Canvas>();
            canvasRect = canvas.GetComponent<RectTransform>();
            distanceText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (pathStartObjectRenderer == null && pathStartObject != null)
            pathStartObjectRenderer = pathStartObject.GetComponent<Renderer>();
        if (pathEndObjectRenderer == null && pathEndObject != null)
            pathEndObjectRenderer = pathEndObject.GetComponent<Renderer>();
        CheckVariables();
    }

    void CheckVariables()
    {
        if (canvas == null)
        {
            Debug.LogError("Canvas not found");
            return;
        }
        if (canvasRect == null)
        {
            Debug.LogError("Canvas rect not found");
            return;
        }
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
    }

    public void SetVisible(bool visible)
    {
        this.visible = visible;
        canvas.enabled = visible;
        pathStartObject.gameObject.SetActive(visible);
        pathEndObject.gameObject.SetActive(visible);
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
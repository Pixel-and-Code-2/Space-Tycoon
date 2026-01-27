using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PawnMoveUIController : PathDrawer
{

    [SerializeField]
    private GameObject canvasObject;
    private Canvas canvas;
    private RectTransform canvasRect;
    private TextMeshProUGUI distanceText;
    [SerializeField]
    private Vector2 distanceTextOffset = new Vector2(0f, 10f);
    [SerializeField]
    protected Color textColor = Color.green;
    [SerializeField]
    protected Color outOfRangeTextColor = Color.red;

    public new void SetPathPoints(Vector3[] pointsAvailable, Vector3[] pointsOutOfRange, Vector2 screenPoint)
    {
        base.SetPathPoints(pointsAvailable, pointsOutOfRange, screenPoint);

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

    protected override void ColorEndTo(bool isOutOfRange = true)
    {
        if (isOutOfRange)
        {
            distanceText.color = outOfRangeTextColor;
        }
        else
        {
            distanceText.color = textColor;
        }
    }

    new void Awake()
    {
        canvas = canvasObject.GetComponent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        distanceText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        base.Awake();
    }

    new void OnValidate()
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

        base.OnValidate();
    }

    protected new void CheckVariables()
    {
        Debug.Log("Checking variables");
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
        base.CheckVariables();
    }

    public override void SetVisible(bool visible)
    {
        canvas.enabled = visible;
        base.SetVisible(visible);
    }

}
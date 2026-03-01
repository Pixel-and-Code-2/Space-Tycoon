using UnityEngine;
using TMPro;

public class PathDrawerWithText : PathDrawer
{

    [SerializeField]
    private GameObject canvasObject;
    private Canvas canvas;
    private RectTransform canvasRect;
    [SerializeField]
    private TextMeshProUGUI uiText;
    [SerializeField]
    private Vector2 uiTextOffset = new Vector2(0f, 10f);
    [SerializeField]
    protected Color textColor = Color.green;
    [SerializeField]
    protected Color outOfRangeTextColor = Color.red;

    public void SetText(string text, Vector2 screenPoint)
    {
        uiText.text = text;
        RectTransform textRect = uiText.rectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.worldCamera,
            out Vector2 localPoint
        );
        textRect.anchoredPosition = localPoint + uiTextOffset;
    }

    new void Awake()
    {
        canvas = canvasObject.GetComponent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        base.Awake();
    }

    new void OnValidate()
    {
        if (
            (canvas == null && canvasObject != null) ||
            (canvasRect == null && canvas != null) ||
            (uiText == null && canvas != null)
        )
        {
            canvas = canvasObject.GetComponent<Canvas>();
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        base.OnValidate();
    }

    protected new void CheckVariables()
    {
        base.CheckVariables();
    }

    public override void SetVisible(bool visible)
    {
        uiText.enabled = visible;
        base.SetVisible(visible);
    }

    public void SetTextColor(Color color)
    {
        uiText.color = color;
    }

}
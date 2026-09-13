using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class PlayerIconController : MonoBehaviour
{
    [SerializeField]
    private GameObject DisableFG;
    [SerializeField]
    private GameObject Selected;
    [SerializeField]
    private GameObject NotSelected;
    [SerializeField]
    private TextMeshProUGUI playerHealingNumber;
    [SerializeField]
    private Button button;
    [SerializeField]
    private float popupDuration = 1.4f;
    [SerializeField]
    private float popupRise = 36f;
    [SerializeField]
    private float popupFontSize = 16f;

    TextMeshProUGUI popupInstance;
    RectTransform popupRoot;

    void OnEnable()
    {
        button = GetComponent<Button>();
    }

    public void UpdateState(PlayerIconState st)
    {
        switch (st)
        {
            case PlayerIconState.Disable:
                DisableFG.SetActive(true);
                Selected.SetActive(false);
                NotSelected.SetActive(true);
                button.interactable = false;
                break;
            case PlayerIconState.Selected:
                DisableFG.SetActive(false);
                Selected.SetActive(true);
                NotSelected.SetActive(false);
                button.interactable = true;
                break;
            case PlayerIconState.NotSelected:
                DisableFG.SetActive(false);
                Selected.SetActive(false);
                NotSelected.SetActive(true);
                button.interactable = true;
                break;
        }
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    public void UpdatePlayer(GameUI.PlayerGroup player)
    {
        if (playerHealingNumber == null || player.playerObject == null) return;
        float amountOfHealings = player.playerObject.GetDynamicParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY);
        float maxHealings = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
        playerHealingNumber.text = (maxHealings - amountOfHealings).ToString("0");
    }

    public void ShowPopup(string message, Color color)
    {
        StopAllCoroutines();
        StartCoroutine(PopupRoutine(message, color));
    }

    Canvas FindOverlayCanvas()
    {
        Canvas local = GetComponentInParent<Canvas>();
        if (local == null) return null;
        return local.rootCanvas != null ? local.rootCanvas : local;
    }

    IEnumerator PopupRoutine(string message, Color color)
    {
        Canvas canvas = FindOverlayCanvas();
        if (canvas == null) yield break;

        if (popupInstance == null)
        {
            GameObject go = new GameObject("IconPopupFloat", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
            popupRoot = go.GetComponent<RectTransform>();
            popupRoot.SetParent(canvas.transform, false);
            Canvas popupCanvas = go.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 500;
            popupInstance = go.GetComponent<TextMeshProUGUI>();
            popupInstance.fontSize = popupFontSize;
            popupInstance.enableAutoSizing = false;
            popupInstance.alignment = TextAlignmentOptions.Left;
            popupInstance.raycastTarget = false;
            popupInstance.textWrappingMode = TextWrappingModes.NoWrap;
            popupInstance.overflowMode = TextOverflowModes.Overflow;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        RectTransform iconRt = transform as RectTransform;
        Vector2 screen;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        screen = RectTransformUtility.WorldToScreenPoint(cam, iconRt.position);
        RectTransform canvasRt = canvas.transform as RectTransform;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, cam, out local);

        popupRoot.anchorMin = new Vector2(0.5f, 0.5f);
        popupRoot.anchorMax = new Vector2(0.5f, 0.5f);
        popupRoot.pivot = new Vector2(0f, 0.5f);
        popupRoot.sizeDelta = new Vector2(280f, 28f);
        popupRoot.anchoredPosition = local + new Vector2(36f, 8f);
        popupInstance.text = message;
        popupInstance.color = color;
        popupInstance.gameObject.SetActive(true);

        float t = 0f;
        Color c = color;
        Vector2 start = popupRoot.anchoredPosition;
        while (t < popupDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / popupDuration);
            popupRoot.anchoredPosition = start + new Vector2(0f, popupRise * u);
            c.a = 1f - u;
            popupInstance.color = c;
            yield return null;
        }
        popupInstance.gameObject.SetActive(false);
    }
}

public enum PlayerIconState
{
    Disable,
    Selected,
    NotSelected
}

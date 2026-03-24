using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NUnit.Framework;

[System.Serializable]
public class MessageObject
{
    public GameObject gameObject;
    public TextMeshProUGUI text;
    public RectTransform rectTransform;
    public bool isBusy => gameObject.activeSelf;
    [SerializeField, HideInInspector]
    public float timeShown = 0f;
    [SerializeField, HideInInspector]
    public Vector3 worldPosition;
}

public class UI3DManager : MonoBehaviour
{
    private class MessageItem
    {
        public string message;
        public Vector3 position;
        public Color color;
    }
    public static UI3DManager Instance { get; private set; }
    [SerializeField]
    private GameObject sliderPrefab;
    [SerializeField]
    private GameObject transformSliderPrefab;
    [SerializeField]
    private Transform sliderParent;
    [SerializeField]
    private GameObject actionBoxPrefab;
    [SerializeField]
    private Transform actionBoxParent;
    [SerializeField]
    private ContextMenuController contextMenuController;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private List<MessageObject> messageObjectList = new List<MessageObject>();
    [SerializeField]
    private float messageDuration = 1f;
    [SerializeField]
    private float messageGoUpOn = 5f;
    private RectTransform canvasRectTransformCached;
    private Queue<MessageItem> messageItems = new Queue<MessageItem>();

    [SerializeField]
    private Vector3 uiOffset = new Vector3(0f, 1f, 0f);

    private Dictionary<GameObject, SliderToPawnConnector> pawnsInScene = new Dictionary<GameObject, SliderToPawnConnector>();
    private Dictionary<ISelectable, SelectableToBoxConnector> selectablesInScene = new Dictionary<ISelectable, SelectableToBoxConnector>();
    private Dictionary<Transform, SliderController> slidersOnTransform = new Dictionary<Transform, SliderController>();

    void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Debug.LogError("Awake met second UI3DManager instance " + Instance.name + " and " + this.name);
        }
        Instance = this;
        if (contextMenuController == null) contextMenuController = GetComponentInChildren<ContextMenuController>();
        contextMenuController.gameObject.SetActive(false);
        canvasRectTransformCached = canvas.GetComponent<RectTransform>();
    }
    void Start()
    {
        Reset();
    }
    void Reset()
    {
        SliderToPawnConnector[] sliderToPawnConnectors = sliderParent.GetComponentsInChildren<SliderToPawnConnector>();
        // Check if all sliders are registered, others will be destroyed
        foreach (SliderToPawnConnector sliderToPawnConnector in sliderToPawnConnectors)
        {
            if (sliderToPawnConnector.pawn.gameObject == null || !pawnsInScene.ContainsKey(sliderToPawnConnector.pawn.gameObject))
            {
                Destroy(sliderToPawnConnector.gameObject);
            }
        }
        // Check if all pawns are registered, others will be re-registered
        foreach (GameObject pawnObject in pawnsInScene.Keys)
        {
            if (pawnsInScene[pawnObject] == null)
            {
                pawnsInScene[pawnObject] = CreateSliderForPawn(pawnObject);
                Debug.LogWarning("Reset: Slider is null, creating new one");
            }
            if (pawnsInScene[pawnObject].pawn != pawnObject.GetComponent<PawnDataController>())
            {
                Destroy(pawnsInScene[pawnObject].gameObject);
                pawnsInScene[pawnObject] = CreateSliderForPawn(pawnObject);
                Debug.LogWarning("Reset: PawnDataController changed, destroying old slider and creating new one");
            }
        }
    }
    void Update()
    {
        UpdateSliderPositions();
        UpdateMessagePosition();
        UpdateTransformSlidersPositions();
        UpdateActionBoxPositions();

        if (contextMenuController.gameObject.activeSelf)
        {
            contextMenuController.UpdateAttach(canvas);
        }
    }
    private float MessageEaseOutFunc(float t)
    {
        return 1 - Mathf.Pow(1 - t, 4);
    }
    private void UpdateMessagePosition()
    {
        if (messageItems.Count > 0 && messageObjectList.Find(m => !m.isBusy) != null)
        {
            MessageObject messageObject = messageObjectList.Find(m => !m.isBusy);

            messageObject.gameObject.SetActive(true);
            messageObject.text.text = messageItems.Peek().message;
            messageObject.text.color = messageItems.Peek().color;
            messageObject.worldPosition = messageItems.Peek().position;
            messageObject.timeShown = 0f;
            messageItems.Dequeue();
        }
        foreach (MessageObject messageObject in messageObjectList)
        {
            if (!messageObject.isBusy) continue;
            Vector3 worldPosition = messageObject.worldPosition + uiOffset + Vector3.up * messageGoUpOn * messageObject.timeShown / messageDuration;
            messageObject.text.color = new Color(
                messageObject.text.color.r,
                messageObject.text.color.g,
                messageObject.text.color.b,
                MessageEaseOutFunc(1 - messageObject.timeShown / messageDuration)
            );
            Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransformCached,
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                messageObject.rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
            messageObject.timeShown += Time.deltaTime;
            if (messageObject.timeShown >= messageDuration)
            {
                messageObject.gameObject.SetActive(false);
                messageObject.timeShown = 0f;
            }
        }
    }

    private void UpdateSliderPositions()
    {
        foreach (GameObject pawnObject in pawnsInScene.Keys)
        {
            SliderToPawnConnector controller = pawnsInScene[pawnObject];
            RectTransform uiElementRect = controller.rectTransform;
            Vector3 worldPosition = pawnObject.transform.position + uiOffset;
            Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransformCached,
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }
    }

    private void UpdateActionBoxPositions()
    {
        foreach (ISelectable selectable in selectablesInScene.Keys)
        {
            // if (!selectablesInScene[selectable].gameObject.activeSelf)
            // {
            //     selectablesInScene[selectable].gameObject.SetActive(true);
            // }
            SelectableToBoxConnector controller = selectablesInScene[selectable];
            RectTransform uiElementRect = controller.rectTransform;
            Vector3 worldPosition = selectable.GetTransform().position + uiOffset;
            Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransformCached,
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }
    }

    private void UpdateTransformSlidersPositions()
    {
        foreach (Transform transform in slidersOnTransform.Keys)
        {
            SliderController controller = slidersOnTransform[transform];
            RectTransform uiElementRect = controller.rectTransform;
            Vector3 worldPosition = transform.position + uiOffset;
            Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransformCached,
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }
    }

    public void RegisterPawn(GameObject pawnObject)
    {
        if (pawnObject == null) throw new System.Exception("RegisterPawn: pawnObject is null");
        if (pawnsInScene.ContainsKey(pawnObject)) return;
        pawnsInScene.Add(pawnObject, CreateSliderForPawn(pawnObject));
    }
    public void UnregisterPawn(GameObject pawnObject)
    {
        if (!pawnsInScene.ContainsKey(pawnObject)) return;
        Destroy(pawnsInScene[pawnObject].gameObject);
        pawnsInScene.Remove(pawnObject);
    }

    public SliderController RegisterSlider(Transform transform)
    {
        if (slidersOnTransform.ContainsKey(transform)) return slidersOnTransform[transform];
        SliderController sliderController = CreateSliderForTransform(transform);
        slidersOnTransform.Add(transform, sliderController);
        return sliderController;
    }

    public void UnregisterSlider(Transform transform)
    {
        if (!slidersOnTransform.ContainsKey(transform)) return;
        Destroy(slidersOnTransform[transform].gameObject);
        slidersOnTransform.Remove(transform);
    }

    public SelectableToBoxConnector RegisterSelectable(ISelectable selectable, string text)
    {
        if (selectablesInScene.ContainsKey(selectable)) return selectablesInScene[selectable];
        SelectableToBoxConnector selectableToBoxConnector = CreateActionBoxForSelectable(selectable, text);
        // selectableToBoxConnector.gameObject.SetActive(false);
        selectablesInScene.Add(selectable, selectableToBoxConnector);
        UpdateActionBoxPositions();
        return selectableToBoxConnector;
    }

    public void UnregisterSelectable(ISelectable selectable)
    {
        if (!selectablesInScene.ContainsKey(selectable)) return;
        Destroy(selectablesInScene[selectable].gameObject);
        selectablesInScene.Remove(selectable);
    }

    public void ShowContextMenu(Vector3 position, List<ContextMenuItem> items)
    {
        contextMenuController.ClearButtons();
        contextMenuController.AddButtons(items);
        ShowContextMenu(position);
    }
    public void ShowContextMenu(Vector3 position)
    {
        contextMenuController.attachToPosition = position;
        contextMenuController.gameObject.SetActive(true);
    }

    public void HideContextMenu()
    {
        contextMenuController.gameObject.SetActive(false);
    }

    private SliderToPawnConnector CreateSliderForPawn(GameObject pawnObject)
    {
        GameObject sliderObject = Instantiate(sliderPrefab, sliderParent);
        SliderToPawnConnector sliderToPawnConnector = sliderObject.GetComponent<SliderToPawnConnector>();
        sliderToPawnConnector.pawn = pawnObject.GetComponent<PawnDataController>();
        return sliderToPawnConnector;
    }

    private SliderController CreateSliderForTransform(Transform transform)
    {
        GameObject sliderObject = Instantiate(transformSliderPrefab, sliderParent);
        SliderController sliderController = sliderObject.GetComponent<SliderController>();
        return sliderController;
    }

    private SelectableToBoxConnector CreateActionBoxForSelectable(ISelectable selectable, string text)
    {
        GameObject actionBoxObject = Instantiate(actionBoxPrefab, actionBoxParent);
        SelectableToBoxConnector selectableToBoxConnector = actionBoxObject.GetComponent<SelectableToBoxConnector>();
        selectableToBoxConnector.selectable = selectable;
        selectableToBoxConnector.text = text;
        return selectableToBoxConnector;
    }

    public void ShowMessage(string message, Vector3 position, Color color)
    {
        messageItems.Enqueue(new MessageItem { message = message, position = position, color = color });
    }
}

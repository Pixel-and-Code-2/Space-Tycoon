using System.Collections.Generic;
using UnityEngine;
using TMPro;
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
    private ContextMenuController contextMenuController;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private GameObject messageObject = null;
    [SerializeField]
    private float messageDuration = 1f;
    private GameObject messageObjectCached = null;
    private RectTransform messageRectTransform;
    private TextMeshProUGUI messageText;
    private Queue<MessageItem> messageItems = new Queue<MessageItem>();

    [SerializeField]
    private Vector3 uiOffset = new Vector3(0f, 1f, 0f);

    private Dictionary<GameObject, SliderToPawnConnector> pawnsInScene = new Dictionary<GameObject, SliderToPawnConnector>();
    private Dictionary<Transform, SliderController> slidersOnTransform = new Dictionary<Transform, SliderController>();

    void OnValidate()
    {
        if (messageObject == null) messageObject = GetComponentInChildren<TextMeshProUGUI>().gameObject;
        if (messageObjectCached != messageObject)
        {
            messageObjectCached = messageObject;
            messageRectTransform = messageObject.GetComponent<RectTransform>();
            messageText = messageObject.GetComponent<TextMeshProUGUI>();
        }
        if (messageObject.activeSelf) messageObject.SetActive(false);
    }

    void Awake()
    {
        if (Instance != this && Instance != null)
        {
            Debug.LogError("Awake met second UI3DManager instance " + Instance.name + " and " + this.name);
        }
        Instance = this;
        if (contextMenuController == null) contextMenuController = GetComponentInChildren<ContextMenuController>();
        contextMenuController.gameObject.SetActive(false);
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

        if (contextMenuController.gameObject.activeSelf)
        {
            contextMenuController.UpdateAttach(canvas);
        }
    }

    private float messageTimeShown = 0f;
    private void UpdateMessagePosition()
    {
        if (messageItems.Count > 0)
        {
            if (!messageObject.activeSelf)
            {
                messageObject.SetActive(true);
                messageText.text = messageItems.Peek().message;
                messageText.color = messageItems.Peek().color;
                messageTimeShown = 0f;
            }
            messageTimeShown += Time.deltaTime;
            if (messageTimeShown >= messageDuration)
            {
                messageObject.SetActive(false);
                messageTimeShown = 0f;
                messageItems.Dequeue();
                return;
            }
            Vector3 worldPosition = messageItems.Peek().position + uiOffset;
            Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                messageRectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }

    }

    private void UpdateSliderPositions()
    {
        foreach (GameObject pawnObject in pawnsInScene.Keys)
        {
            if (pawnObject == null || pawnsInScene[pawnObject] == null)
            {
                Debug.LogWarning("LateUpdate: pawnObject or pawnsInScene[pawnObject] is null, skipping");
                continue;
            }
            SliderToPawnConnector controller = pawnsInScene[pawnObject];
            RectTransform uiElementRect = controller.rectTransform;
            Vector3 worldPosition = pawnObject.transform.position + uiOffset;
            Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(worldPosition);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
            else
            {
                Debug.LogWarning($"Could not convert screen point to local point for {pawnObject.name}");
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
                canvas.GetComponent<RectTransform>(),
                new Vector2(screenPosition.x, screenPosition.y),
                canvas.worldCamera,
                out localPoint))
            {
                uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
            else
            {
                Debug.LogWarning($"Could not convert screen point to local point for {transform.name}");
            }
        }
    }

    public void RegisterPawn(GameObject pawnObject)
    {
        if (pawnObject == null) throw new System.Exception("RegisterPawn: pawnObject is null");
        if (pawnsInScene.ContainsKey(pawnObject)) return;
        pawnsInScene.Add(pawnObject, CreateSliderForPawn(pawnObject));
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

    public void ShowMessage(string message, Vector3 position, Color color)
    {
        messageItems.Enqueue(new MessageItem { message = message, position = position, color = color });
    }
}

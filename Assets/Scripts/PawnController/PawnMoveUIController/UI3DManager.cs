using System.Collections.Generic;
using UnityEngine;

public class UI3DManager : MonoBehaviour
{
    public static UI3DManager Instance { get; private set; }
    [SerializeField]
    private GameObject sliderPrefab;
    [SerializeField]
    private Transform sliderParent;
    [SerializeField]
    private ContextMenuController contextMenuController;
    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    private Vector3 uiOffset = new Vector3(0f, 1f, 0f);

    private Dictionary<GameObject, SliderToPawnConnector> pawnsInScene = new Dictionary<GameObject, SliderToPawnConnector>();

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
                // uiElementRect.localPosition = localPoint;
                uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0.1f);
            }
            else
            {
                Debug.LogWarning($"Could not convert screen point to local point for {pawnObject.name}");
            }
            // pawnsInScene[pawnObject].rectTransform.position = canvas.worldCamera.WorldToScreenPoint(pawnObject.transform.position + uiOffset);
        }

        if (contextMenuController.gameObject.activeSelf)
        {
            contextMenuController.UpdateAttach(canvas);
        }
    }

    public void RegisterPawn(GameObject pawnObject)
    {
        if (pawnObject == null) throw new System.Exception("RegisterPawn: pawnObject is null");
        if (pawnsInScene.ContainsKey(pawnObject)) return;
        pawnsInScene.Add(pawnObject, CreateSliderForPawn(pawnObject));
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
        if (sliderToPawnConnector == null) throw new System.Exception("CreateSliderForPawn: sliderToPawnConnector not found");
        sliderToPawnConnector.pawn = pawnObject.GetComponent<PawnDataController>();
        if (sliderToPawnConnector.pawn == null) throw new System.Exception("CreateSliderForPawn: PawnDataController not found");
        return sliderToPawnConnector;
    }
}

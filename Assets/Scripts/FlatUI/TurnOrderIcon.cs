using UnityEngine;
using UnityEngine.UI;

public class TurnOrderIcon : MonoBehaviour
{
    [SerializeField]
    private GameObject smallIcon;
    [SerializeField]
    private GameObject bigIcon;
    [SerializeField]
    private Image smallImage;
    [SerializeField]
    private Image bigImage;
    [SerializeField]
    private Button button;

    private IControlableSelectable boundPawn;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }
    }

    public void Bind(IControlableSelectable pawn, Sprite sprite)
    {
        boundPawn = pawn;
        if (smallImage != null) smallImage.sprite = sprite;
        if (bigImage != null) bigImage.sprite = sprite;
        SetCurrent(false);
    }

    public void SetCurrent(bool isCurrent)
    {
        if (smallIcon != null) smallIcon.SetActive(!isCurrent);
        if (bigIcon != null) bigIcon.SetActive(isCurrent);
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    public IControlableSelectable BoundPawn => boundPawn;

    private void OnClicked()
    {
        if (boundPawn == null || CameraTargetController.Instance == null) return;
        CameraTargetController.Instance.FocusOnLookTarget(boundPawn);
    }
}

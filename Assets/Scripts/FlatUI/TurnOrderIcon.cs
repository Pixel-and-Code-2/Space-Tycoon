using UnityEngine;
using UnityEngine.UI;

public class TurnOrderIcon : MonoBehaviour
{
    [SerializeField]
    private GameObject smallIcon;
    [SerializeField]
    private GameObject bigIcon;
    [SerializeField]
    private TopIconStack smallStack;
    [SerializeField]
    private TopIconStack bigStack;
    [SerializeField]
    private Button button;

    private IControlableSelectable boundPawn;

    void Awake()
    {
        if (smallStack == null && smallIcon != null)
            smallStack = smallIcon.GetComponent<TopIconStack>() ?? smallIcon.AddComponent<TopIconStack>();
        if (bigStack == null && bigIcon != null)
            bigStack = bigIcon.GetComponent<TopIconStack>() ?? bigIcon.AddComponent<TopIconStack>();
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }
    }

    public void Bind(IControlableSelectable pawn, TurnOrderPortrait portrait)
    {
        boundPawn = pawn;
        if (portrait == null && pawn != null)
            portrait = TurnOrderPortrait.GetFromPawn(pawn);
        if (smallStack != null) smallStack.Apply(portrait);
        if (bigStack != null) bigStack.Apply(portrait);
        SetCurrent(false);
    }

    public void SetCurrent(bool isCurrent)
    {
        if (smallIcon != null) smallIcon.SetActive(!isCurrent);
        if (bigIcon != null) bigIcon.SetActive(isCurrent);
        RebuildLayout();
    }

    void RebuildLayout()
    {
        var rt = transform as RectTransform;
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        if (transform.parent is RectTransform parentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    public IControlableSelectable BoundPawn => boundPawn;

    private void OnClicked()
    {
        if (boundPawn == null || CameraTargetController.Instance == null) return;
        CameraTargetController.Instance.FocusOnLookTarget(boundPawn);
    }
}

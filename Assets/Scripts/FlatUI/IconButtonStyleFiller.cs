using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(ButtonStopPropagation))]
public class IconButtonStyleFiller : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private Button button;
    [SerializeField]
    private string styleName;
    [SerializeField]
    private bool isMirrored = false;
    [SerializeField]
    private Image bg;
    [SerializeField]
    private Image mg;
    [SerializeField]
    private Image fg;
    [SerializeField]
    private Image bgHighlightAddition;
    [SerializeField]
    private Image mgHighlightAddition;
    [SerializeField]
    private Image fgHighlightAddition;
    [SerializeField]
    private Image bgPressedAddition;
    [SerializeField]
    private Image mgPressedAddition;
    [SerializeField]
    private Image fgPressedAddition;
    public bool IsButtonOn => isOnCache == 1;
    public bool IsButtonHighlighted => isHighlightedCache == 1;
    public bool IsButtonInteractable => isInteractableCache == 1;
    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {

        OnValidate();
    }
    void OnValidate()
    {
        if (isMirrored)
        {
            if (bg != null) bg.transform.localScale = new Vector3(-1, 1, 1);
            if (mg != null) mg.transform.localScale = new Vector3(-1, 1, 1);
            if (fg != null) fg.transform.localScale = new Vector3(-1, 1, 1);
            if (bgHighlightAddition != null) bgHighlightAddition.transform.localScale = new Vector3(-1, 1, 1);
            if (mgHighlightAddition != null) mgHighlightAddition.transform.localScale = new Vector3(-1, 1, 1);
            if (fgHighlightAddition != null) fgHighlightAddition.transform.localScale = new Vector3(-1, 1, 1);
            if (bgPressedAddition != null) bgPressedAddition.transform.localScale = new Vector3(-1, 1, 1);
            if (mgPressedAddition != null) mgPressedAddition.transform.localScale = new Vector3(-1, 1, 1);
            if (fgPressedAddition != null) fgPressedAddition.transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            if (bg != null) bg.transform.localScale = new Vector3(1, 1, 1);
            if (mg != null) mg.transform.localScale = new Vector3(1, 1, 1);
            if (fg != null) fg.transform.localScale = new Vector3(1, 1, 1);
            if (bgHighlightAddition != null) bgHighlightAddition.transform.localScale = new Vector3(1, 1, 1);
            if (mgHighlightAddition != null) mgHighlightAddition.transform.localScale = new Vector3(1, 1, 1);
            if (fgHighlightAddition != null) fgHighlightAddition.transform.localScale = new Vector3(1, 1, 1);
            if (bgPressedAddition != null) bgPressedAddition.transform.localScale = new Vector3(1, 1, 1);
            if (mgPressedAddition != null) mgPressedAddition.transform.localScale = new Vector3(1, 1, 1);
            if (fgPressedAddition != null) fgPressedAddition.transform.localScale = new Vector3(1, 1, 1);
        }
        TurnOnButton();
    }
    public void TurnOnButton()
    {
        UpdateButton(1, -1, -1);
    }
    public void SetInteractable(bool interactable)
    {
        UpdateButton(-1, -1, interactable ? 1 : 0);
    }
    public void TurnOffButton()
    {
        UpdateButton(0, -1, -1);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateButton(-1, 1, -1);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateButton(-1, 0, -1);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateButton(-1, -1, -1, 1);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        UpdateButton(-1, -1, 1, 0);
    }
    int isOnCache = 1;
    int isHighlightedCache = 0;
    int isInteractableCache = 1;
    int isPressedCache = 0;
    private void UpdateButton(int isOn = -1, int isHighlighted = -1, int isInteractable = -1, int isPressed = -1)
    {
        if (isOn == -1) isOn = isOnCache;
        else isOnCache = isOn;
        if (isHighlighted == -1) isHighlighted = isHighlightedCache;
        else isHighlightedCache = isHighlighted;
        if (isInteractable == -1) isInteractable = isInteractableCache;
        else isInteractableCache = isInteractable;
        if (isPressed == -1) isPressed = isPressedCache;
        else isPressedCache = isPressed;
        if (HandleInittingGlobalVars.globalSettingsAssets == null) return;
        var style = HandleInittingGlobalVars.globalSettingsAssets.GetIconButtonStyle(styleName);
        if (style == null) return;
        if (isOn == 1)
        {
            TrySetSprite(bg, style.bgOn);
            TrySetSprite(mg, style.mgOn);
            TrySetSprite(fg, style.fgOn);
        }
        else
        {
            TrySetSprite(bg, style.bgOff);
            TrySetSprite(mg, style.mgOff);
            TrySetSprite(fg, style.fgOff);
        }
        if (isHighlighted == 1)
        {
            TrySetSprite(bgHighlightAddition, style.bgHighlightAddition);
            TrySetSprite(mgHighlightAddition, style.mgHighlightAddition);
            TrySetSprite(fgHighlightAddition, style.fgHighlightAddition);
        }
        else
        {
            if (bgHighlightAddition != null)
                bgHighlightAddition.enabled = false;
            if (mgHighlightAddition != null)
                mgHighlightAddition.enabled = false;
            if (fgHighlightAddition != null)
                fgHighlightAddition.enabled = false;
        }
        if (isPressed == 1)
        {
            TrySetSprite(bgPressedAddition, style.bgPressedAddition);
            TrySetSprite(mgPressedAddition, style.mgPressedAddition);
            TrySetSprite(fgPressedAddition, style.fgPressedAddition);
        }
        else
        {
            if (bgPressedAddition != null)
                bgPressedAddition.enabled = false;
            if (mgPressedAddition != null)
                mgPressedAddition.enabled = false;
            if (fgPressedAddition != null)
                fgPressedAddition.enabled = false;
        }
        if (isInteractable == 1)
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }
    private void TrySetSprite(Image image, string spriteName)
    {
        if (image == null) return;
        if (spriteName == string.Empty)
        {
            image.enabled = false;
            return;
        }
        var spriteLink = HandleInittingGlobalVars.globalSettingsAssets.GetSpriteLink(spriteName);
        if (spriteLink != null)
        {
            image.sprite = spriteLink.sprite;
            image.enabled = true;
            if (spriteLink.sprite != null && spriteLink.sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
            }
        }
        else image.enabled = false;
    }
}
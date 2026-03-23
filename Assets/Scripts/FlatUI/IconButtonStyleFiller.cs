using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(ButtonStopPropagation))]
public class IconButtonStyleFiller : MonoBehaviour
{
    [SerializeField]
    private Button button;
    [SerializeField]
    private string styleName;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        TurnOnButton();
    }
    public void TurnOnButton()
    {
        button.interactable = true;
        var style = HandleInittingGlobalVars.globalSettingsAssets.GetIconButtonStyle(styleName);
        if (style != null)
        {
            if (style.spriteOn != null)
            {
                if (button.image.sprite != style.spriteOn)
                {
                    button.image.sprite = style.spriteOn;
                }
            }
            if (style.colorOn != Color.white)
            {
                button.image.color = style.colorOn;
            }
        }
    }
    public void TurnOffButton()
    {
        button.interactable = false;
        var style = HandleInittingGlobalVars.globalSettingsAssets.GetIconButtonStyle(styleName);
        if (style != null)
        {
            if (style.spriteOff != null)
            {
                if (button.image.sprite != style.spriteOff)
                {
                    button.image.sprite = style.spriteOff;
                }
            }
            if (style.colorOff != Color.white)
            {
                button.image.color = style.colorOff;
            }
        }
    }
}
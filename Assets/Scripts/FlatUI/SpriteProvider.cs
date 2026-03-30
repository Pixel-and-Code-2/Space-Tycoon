using UnityEngine;
using UnityEngine.UI;

public class SpriteProvider : MonoBehaviour
{
    [SerializeField]
    private string spriteLinkName;
    [SerializeField]
    private Image image;
    [SerializeField]
    private string colorLinkName;
    void Start()
    {
        UpdateImage();
    }
    void OnValidate()
    {
        UpdateImage();
    }
    void UpdateImage()
    {
        if (spriteLinkName == null || spriteLinkName == "") return;
        if (image == null) return;
        if (HandleInittingGlobalVars.globalSettingsAssets == null || HandleInittingGlobalVars.globalSettingsAssets.GetSpriteLink(spriteLinkName) == null) return;
        image.sprite = HandleInittingGlobalVars.globalSettingsAssets.GetSpriteLink(spriteLinkName).sprite;
        if (colorLinkName != null && colorLinkName != "") image.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(colorLinkName).color;
    }
    public Sprite GetSprite()
    {
        return HandleInittingGlobalVars.globalSettingsAssets.GetSpriteLink(spriteLinkName).sprite;
    }
}
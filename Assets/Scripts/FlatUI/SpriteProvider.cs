using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.U2D;

public class SpriteProvider : MonoBehaviour
{
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
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
        if (string.IsNullOrEmpty(spriteLinkName)) return;
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;

        if (spriteCache.ContainsKey(spriteLinkName) && (spriteCache[spriteLinkName] == null || spriteCache[spriteLinkName].texture == null))
        {
            spriteCache.Remove(spriteLinkName);
        }
        if (spriteCache.ContainsKey(spriteLinkName))
        {
            image.sprite = spriteCache[spriteLinkName];
        }
        else if (spriteLinkName != null && spriteLinkName != "")
        {
            Sprite sprite = Resources.Load<Sprite>("Sprites/" + spriteLinkName);
            if (sprite == null)
            {
                bool found = false;
                Object[] sprites = Resources.LoadAll<Sprite>("Sprites/Atlas_04");
                foreach (var obj in sprites)
                {
                    if (obj is Sprite s && !spriteCache.ContainsKey(s.name))
                    {
                        spriteCache[s.name] = s;
                        if (s.name == spriteLinkName)
                        {
                            image.sprite = s;
                            found = true;
                        }
                    }
                }
                if (!found)
                    Debug.LogError($"{name}: Sprite {spriteLinkName} not found in Resources/Sprites");
                return;
            }
            image.sprite = sprite;
            spriteCache[spriteLinkName] = sprite;

        }
        if (image == null || HandleInittingGlobalVars.globalSettingsAssets == null) return;
        if (colorLinkName != null && colorLinkName != "") image.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(colorLinkName).color;
        else image.color = Color.white;
    }
}
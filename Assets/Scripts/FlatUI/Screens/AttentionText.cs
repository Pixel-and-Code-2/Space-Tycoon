using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class AttentionText : IUILayer
{
    [SerializeField]
    TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    Image backgroundImage;
    [SerializeField]
    float duration = 1f;
    [SerializeField]
    private GameObject onlyPersistentObject;
    private float timeElapsed = 0f;
    private bool isPersistent = false;
    [SerializeField]
    private List<string> bgLinks = new List<string>();
    public override void Initialize(string config)
    {
        string[] parts = config.Split('_');
        if (parts.Length > 1 && parts[1] == "persistent")
        {
            isPersistent = true;
            onlyPersistentObject.SetActive(true);
        }
        else
        {
            isPersistent = false;
            onlyPersistentObject.SetActive(false);
        }
        int index = -1;
        if (parts.Length > 2)
        {
            index = int.Parse(parts[2]);
        }
        if (parts.Length > 3)
        {
            string color = parts[3];
            if (color != "")
            {
                textMeshProUGUI.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(color).color;
            }
        }
        if (index != -1)
        {
            var temp = Resources.Load<Sprite>("Screens/" + bgLinks[index]);
            if (temp != null)
            {
                backgroundImage.sprite = temp;
            }
        }
        if (parts.Length > 0)
        {
            textMeshProUGUI.text = parts[0];
        }
        else
        {
            textMeshProUGUI.text = "";
        }
    }
    void OnEnable()
    {
        timeElapsed = 0f;
    }
    void Update()
    {
        if (isPersistent) return;
        timeElapsed += Time.unscaledDeltaTime;
        if (timeElapsed >= duration)
        {
            UILayersController.Instance.GoBack();
        }
    }
    public void OnExit()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.MainMenu);
        // Debug.Log("Going back to main menu");
    }
}
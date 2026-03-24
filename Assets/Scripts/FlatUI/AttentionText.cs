using UnityEngine;
using TMPro;

public class AttentionText : IUILayer
{
    [SerializeField]
    TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    float duration = 1f;
    [SerializeField]
    private GameObject onlyPersistentObject;
    private float timeElapsed = 0f;
    private bool isPersistent = false;
    public override void Initialize(string config)
    {
        if (config.EndsWith("_persistent"))
        {
            isPersistent = true;
            onlyPersistentObject.SetActive(true);
        }
        else
        {
            isPersistent = false;
            onlyPersistentObject.SetActive(false);
        }
        textMeshProUGUI.text = config.Replace("_persistent", "");
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
            UILayersController.Instance.SetLayer(UILayersController.UILayer.GameUI);
        }
    }
    public void OnExit()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.MainMenu);
    }
}
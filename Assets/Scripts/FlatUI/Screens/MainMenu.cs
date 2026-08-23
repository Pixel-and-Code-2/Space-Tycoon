using UnityEngine;
using TMPro;

public class MainMenu : IUILayer
{
    [SerializeField]
    private TextMeshProUGUI clickableText;
    [SerializeField]
    private AnimationCurve textOpacity = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
    [SerializeField, Range(0f, 10f)]
    private float animationDuration = 1f;
    void OnEnable()
    {
        gameObject.SetActive(true);
        if (TurnManager.Instance != null)
            TurnManager.Instance.AbortCombatForMenu();
        AudioController.Instance.Play(AudioController.Instance.mainMenuAmbient, true);
    }
    void OnDisable()
    {
        gameObject.SetActive(false);
    }
    public void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnPlay()
    {
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.SaveGame, "gameStart");
    }
    public void OnSettings()
    {
        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.Settings);
    }

    void Update()
    {
        clickableText.alpha = textOpacity.Evaluate((Time.unscaledTime / animationDuration) % 1f);
    }
    public void OnTitles()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.CutScene, "titles_menu");
    }
}
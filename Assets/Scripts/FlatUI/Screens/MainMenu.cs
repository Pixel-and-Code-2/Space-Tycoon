using UnityEngine;

public class MainMenu : IUILayer
{
    void OnEnable()
    {
        gameObject.SetActive(true);
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
}
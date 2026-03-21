using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    public bool isMainMenuVisible => gameObject.activeSelf;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Constructor met second MainMenu instance");
        }
        gameObject.SetActive(false);
    }
    public void ToggleMainMenu()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        }
        else
        {
            gameObject.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class GameUI : IUILayer
{
    [SerializeField]
    private InputActionReference togglePause;
    void OnEnable()
    {
        gameObject.SetActive(true);
        togglePause.action.Enable();
    }
    void OnDisable()
    {
        gameObject.SetActive(false);
        togglePause.action.Disable();
    }
    void Update()
    {
        if (togglePause.action.triggered)
        {
            OnPause();
        }
    }
    public void OnPause()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.PauseMenu);
    }
    public void OnHelp()
    {
        // UILayersController.Instance.SetLayer(UILayersController.UILayer.HelpMenu);
    }
}
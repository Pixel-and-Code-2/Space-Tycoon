using UnityEngine;
using UnityEngine.InputSystem;

public class SaveControllerTest : MonoBehaviour
{
    [SerializeField]
    private InputActionReference saveAction;
    [SerializeField]
    private InputActionReference loadAction;
    [SerializeField]
    private InputActionReference showLastSavedDataAction;

    private void Awake()
    {
        saveAction.action.performed += Save;
        loadAction.action.performed += Load;
        saveAction.action.Enable();
        loadAction.action.Enable();
        showLastSavedDataAction.action.Enable();
    }
    void Start()
    {
        showLastSavedDataAction.action.performed += (context) => SaveHub.Instance.ShowLastSavedData();
    }
    private void Save(InputAction.CallbackContext context)
    {
        SaveHub.Instance.MakeSave();
    }

    private void Load(InputAction.CallbackContext context)
    {
        SaveHub.Instance.LoadAllData();
    }
}
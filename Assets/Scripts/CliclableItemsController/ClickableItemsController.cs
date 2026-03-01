using UnityEngine;
using System.Collections.Generic;

public class ClickableItemsController : MonoBehaviour
{
    public static ClickableItemsController Instance { get; private set; }
    public ISelectable currentSelectedItem { get; private set; }
    [SerializeField]
    private List<ISelectable> taskScenario;
    private int currentTaskScenarioIndex = 0;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("ClickableItemsController instance already exists");
    }

    void OnEnable()
    {
        TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
    }
    void OnDisable()
    {
        TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
    }

    void OnDestroy()
    {
        TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
    }
    public void OnSelect(ISelectable selectable)
    {
        bool selecting = true;
        if (taskScenario.Count != 0)
        {
            selecting = false;
            if (currentTaskScenarioIndex < taskScenario.Count)
            {
                if (taskScenario[currentTaskScenarioIndex] == selectable)
                {
                    selecting = true;
                }
            }
        }
        if (selecting)
        {
            if (currentSelectedItem == null)
            {
                currentSelectedItem = selectable;
                currentSelectedItem.OnSelect();
            }
            else
            {
                if (currentSelectedItem != selectable)
                {
                    OnDeselect();
                    currentSelectedItem = selectable;
                    currentSelectedItem.OnSelect();
                }
            }
        }
    }
    public void OnDeselect()
    {
        if (currentSelectedItem == null) return;
        currentSelectedItem.OnDeselect();
        currentSelectedItem = null;
    }
    public void OnContextMenu()
    {
        if (currentSelectedItem == null) return;
        List<ContextMenuItem> items = currentSelectedItem.OnContextMenu();
        if (items != null)
        {
            UI3DManager.Instance.ShowContextMenu(currentSelectedItem.GetTransform().position, items);
        }
    }

    public void OnPlayerTurnStart()
    {
        if (taskScenario.Count > currentTaskScenarioIndex && !taskScenario[currentTaskScenarioIndex].IsWorking())
            UI3DManager.Instance.ShowMessage("Task " + (currentTaskScenarioIndex + 1), taskScenario[currentTaskScenarioIndex].GetTransform().position, Color.purple);
    }

    public void OnCompleteTask(ISelectable selectable)
    {
        if (currentTaskScenarioIndex < taskScenario.Count && selectable == taskScenario[currentTaskScenarioIndex])
        {
            currentTaskScenarioIndex++;
        }
    }

}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField]
    private RectTransform iconsParent;
    [SerializeField]
    private TurnOrderIcon iconPrefab;
    [SerializeField]
    private Sprite defaultPlayerSprite;
    [SerializeField]
    private Sprite defaultEnemySprite;
    [SerializeField]
    private GameObject rootPanel;

    private readonly List<TurnOrderIcon> spawnedIcons = new List<TurnOrderIcon>();

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnQueueChanged += Refresh;
            TurnManager.Instance.OnTriggerZoneExit += Hide;
        }
        Hide();
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnQueueChanged -= Refresh;
            TurnManager.Instance.OnTriggerZoneExit -= Hide;
        }
    }

    private void Hide()
    {
        ClearIcons();
        if (rootPanel != null) rootPanel.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (TurnManager.Instance == null || iconPrefab == null || iconsParent == null) return;
        var queue = TurnManager.Instance.RoundQueue;
        if (queue == null || queue.Count == 0)
        {
            Hide();
            return;
        }
        if (rootPanel != null) rootPanel.SetActive(true);
        else gameObject.SetActive(true);

        while (spawnedIcons.Count < queue.Count)
        {
            TurnOrderIcon icon = Instantiate(iconPrefab, iconsParent);
            spawnedIcons.Add(icon);
        }
        for (int i = queue.Count; i < spawnedIcons.Count; i++)
            spawnedIcons[i].gameObject.SetActive(false);

        IControlableSelectable current = TurnManager.Instance.CurrentActor;
        for (int i = 0; i < queue.Count; i++)
        {
            TurnOrderIcon icon = spawnedIcons[i];
            icon.gameObject.SetActive(true);
            icon.transform.SetSiblingIndex(i);
            IControlableSelectable pawn = queue[i].pawn;
            icon.Bind(pawn, ResolveSprite(pawn));
            icon.SetCurrent(pawn == current);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(iconsParent);
    }

    private Sprite ResolveSprite(IControlableSelectable pawn)
    {
        if (pawn == null) return defaultEnemySprite;
        TurnOrderPortrait portrait = pawn.GetComponent<TurnOrderPortrait>();
        if (portrait != null && portrait.Portrait != null) return portrait.Portrait;
        if (pawn.GetSelectableType() == SelectableType.Player) return defaultPlayerSprite;
        return defaultEnemySprite;
    }

    private void ClearIcons()
    {
        for (int i = 0; i < spawnedIcons.Count; i++)
        {
            if (spawnedIcons[i] != null)
                spawnedIcons[i].gameObject.SetActive(false);
        }
    }
}

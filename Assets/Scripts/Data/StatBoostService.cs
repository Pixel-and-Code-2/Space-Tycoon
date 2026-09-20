using UnityEngine;

public static class StatBoostService
{
    public static void TryGrantAfterKill(IControlableSelectable killer)
    {
    }

    public static void TryGrantAfterCombat()
    {
    }

    public static void TryGrantAfterTask(IControlableSelectable executor, ClickableItemsController.TaskItem completed)
    {
        if (completed == null) return;
        if (string.IsNullOrEmpty(completed.completeText)) return;
        if (!IsSuccessColor(completed.completeTextColor)) return;
        bool isMain = false;
        if (ClickableItemsController.Instance != null)
        {
            var main = ClickableItemsController.Instance.mainTaskScenario;
            if (main != null)
            {
                for (int i = 0; i < main.Count; i++)
                {
                    if (ReferenceEquals(main[i], completed))
                    {
                        isMain = true;
                        break;
                    }
                }
            }
        }
        PartyProgress.GrantTaskXp(isMain, executor);
    }

    public static bool IsSuccessColor(Color c)
    {
        return c.g >= 0.75f && c.r <= 0.35f && c.b <= 0.35f;
    }
}

using UnityEngine;

public static class ProgressionMessages
{
    public static void ShowXp(int amount, IControlableSelectable pawn = null)
    {
        if (amount <= 0) return;
        string msg = "+" + amount + " hp";
        Color color = new Color(0.2f, 1f, 0.35f);
        if (GameUI.Instance != null)
            GameUI.Instance.ShowXpPopup(msg, color, pawn);
        else if (UI3DManager.Instance != null && pawn != null)
            UI3DManager.Instance.ShowMessage(msg, pawn.GetTransform().position, color, true);
        else if (UI3DManager.Instance != null)
            UI3DManager.Instance.ShowMessage(msg, Vector3.zero, color);
    }
}

using UnityEngine;

public class TurnOrderPortrait : MonoBehaviour
{
    [SerializeField]
    private string portraitSpriteName;
    [SerializeField]
    private string[] portraitSpriteVariants;
    [SerializeField]
    private bool isEnemy;

    private string pickedName;
    private bool picked;

    void Awake()
    {
        PickVariant();
    }

    void PickVariant()
    {
        if (picked) return;
        if (portraitSpriteVariants != null && portraitSpriteVariants.Length > 0)
            pickedName = portraitSpriteVariants[Random.Range(0, portraitSpriteVariants.Length)];
        else
            pickedName = portraitSpriteName;
        picked = true;
    }

    public string PortraitSpriteName
    {
        get
        {
            if (!picked) PickVariant();
            return pickedName;
        }
    }

    public bool IsEnemy => isEnemy;
    public string FrameSpriteName => isEnemy ? "TopIconFrameEnemy" : "TopIconFrameAlly";
    public string BgSpriteName => isEnemy ? "TopIconBGEnemy" : "TopIconBGAlly";

    public static TurnOrderPortrait GetFromPawn(IControlableSelectable pawn)
    {
        if (pawn == null) return null;
        var all = pawn.GetComponents<TurnOrderPortrait>();
        if (all.Length == 0) return null;
        if (all.Length == 1) return all[0];
        for (int i = all.Length - 1; i >= 0; i--)
        {
            if (all[i].portraitSpriteVariants != null && all[i].portraitSpriteVariants.Length > 0)
                return all[i];
        }
        return all[all.Length - 1];
    }
}

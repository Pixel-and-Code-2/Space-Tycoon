using UnityEngine;


public class AnimatorBrainPlayer : AnimatorBrainBase
{
    public new enum Animations
    {
        NONE = 0,
        IDLE = 1,
        WALK = 2,
        ATTACK = 3,
        DEATH = 4,
    }
    protected new readonly static int[] animations =
    {
        0,
        Animator.StringToHash("root|Z_A_IA_IDLE"),
        Animator.StringToHash("root|Z_A_IA_WALK"),
        Animator.StringToHash("root|Z_A_IA_MeleeAtack"),
        Animator.StringToHash("root|Z_A_IA_DEATH"),
    };

    protected override void HandleBypassLock(int layer)
    {
        foreach (var item in animator.GetBehaviours<OnExitPlayer>())
        {
            if (item.layerIndex == layer)
                item.cancel = true;
        }
    }
}
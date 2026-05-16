using UnityEngine;


public class AnimatorBrainZaya : AnimatorBrainBase
{
    public new enum Animations
    {
        NONE = 0,
        IDLE = 1,
        WALK = 2,
        ATTACK = 3,
        DEATH = 4,
        DAMAGE = 5,
    }
    void Awake()
    {
        animations = new int[] {
            0,
            Animator.StringToHash("4_IDLE"),
            Animator.StringToHash("1_MOVE"),
            Animator.StringToHash("2_ATTACK"),
            Animator.StringToHash("3_DEATH"),
            Animator.StringToHash("5_DAMAGE"),
        };
        subAnimations = new int[] {
            0,
            Animator.StringToHash("4_I"),
            Animator.StringToHash("1_M"),
            Animator.StringToHash("2_A"),
            Animator.StringToHash("3_D"),
            Animator.StringToHash("5_D (1)"),
        };
        isSubEnables = true;
    }

    protected override void HandleBypassLock(int layer)
    {
        foreach (var item in animator.GetBehaviours<OnExitZaya>())
        {
            if (item.layerIndex == layer)
                item.cancel = true;
        }
    }
}
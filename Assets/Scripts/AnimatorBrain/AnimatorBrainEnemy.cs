using UnityEngine;

public class AnimatorBrainEnemy : AnimatorBrainBase
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
            Animator.StringToHash("5_HIT"),
        };
        isSubEnables = false;
    }

    protected override void HandleBypassLock(int layer)
    {
        foreach (var item in animator.GetBehaviours<OnExitEnemy>())
        {
            if (item.layerIndex == layer)
                item.cancel = true;
        }
    }
}
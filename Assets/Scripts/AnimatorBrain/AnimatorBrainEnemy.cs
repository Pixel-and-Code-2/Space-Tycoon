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
        DEATH1 = 5,
        RAGE = 6,
    }
    protected new readonly static int[] animations =
    {
        0,
        Animator.StringToHash("root|UFO_Idle"),
        Animator.StringToHash("root|UFO_Walk"),
        Animator.StringToHash("root|UFO_Ataka"),
        Animator.StringToHash("root|UFO_Death"),
        Animator.StringToHash("root|UFO_Death (1)"),
        Animator.StringToHash("root|UFO_Rage")
    };

    protected override void HandleBypassLock(int layer)
    {
        foreach (var item in animator.GetBehaviours<OnExitEnemy>())
        {
            if (item.layerIndex == layer)
                item.cancel = true;
        }
    }
}
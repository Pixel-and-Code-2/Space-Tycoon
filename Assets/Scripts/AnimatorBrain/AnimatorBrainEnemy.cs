//Author: Small Hedge Games
//Date: 21/03/2024

using UnityEngine;
using System;

public class AnimatorBrainEnemy : MonoBehaviour
{
    private readonly static int[] animations =
    {
        0,
        Animator.StringToHash("root|UFO_Idle"),
        Animator.StringToHash("root|UFO_Walk"),
        Animator.StringToHash("root|UFO_Ataka"),
        Animator.StringToHash("root|UFO_Death"),
        Animator.StringToHash("root|UFO_Death (1)"),
        Animator.StringToHash("root|UFO_Rage")
    };

    private Animator animator;
    private EnemyAnimations[] currentAnimation;
    private bool[] layerLocked;
    private Action<int> DefaultAnimation;

    public void Initialize(int layers, EnemyAnimations startingAnimation, Animator animator, Action<int> DefaultAnimation)
    {
        layerLocked = new bool[layers];
        currentAnimation = new EnemyAnimations[layers];
        this.animator = animator;
        this.DefaultAnimation = DefaultAnimation;

        for (int i = 0; i < layers; i++)
        {
            layerLocked[i] = false;
            currentAnimation[i] = startingAnimation;
        }
    }

    public EnemyAnimations GetCurrentAnimation(int layer)
    {
        return currentAnimation[layer];
    }

    public void SetLocked(bool lockLayer, int layer)
    {
        layerLocked[layer] = lockLayer;
    }

    public void Play(EnemyAnimations animation, int layer, bool lockLayer, bool bypassLock, float crossfade = 0.2f)
    {
        if (animation == EnemyAnimations.NONE)
        {
            DefaultAnimation(layer);
            return;
        }

        if (layerLocked[layer] && !bypassLock) return;
        layerLocked[layer] = lockLayer;

        if (bypassLock)
            foreach (var item in animator.GetBehaviours<OnExitEnemy>())
                if (item.layerIndex == layer)
                    item.cancel = true;

        if (currentAnimation[layer] == animation) return;

        currentAnimation[layer] = animation;
        animator.CrossFade(animations[(int)currentAnimation[layer]], crossfade, layer);
    }
}

public enum EnemyAnimations
{
    NONE,
    IDLE,
    WALK,
    ATTACK,
    DEATH,
    DEATH1,
    RAGE
}
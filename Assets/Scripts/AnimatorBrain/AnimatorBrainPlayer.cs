//Author: Small Hedge Games
//Date: 21/03/2024

using UnityEngine;
using System;

public class AnimatorBrainPlayer : MonoBehaviour
{
    private readonly static int[] animations =
    {
        0,
        Animator.StringToHash("root|Z_A_IA_IDLE"),
        Animator.StringToHash("root|Z_A_IA_WALK"),
        Animator.StringToHash("root|Z_A_IA_MeleeAtack"),
        Animator.StringToHash("root|Z_A_IA_DEATH"),
    };

    private Animator animator;
    private PlayerAnimations[] currentAnimation;
    private bool[] layerLocked;
    private Action<int> DefaultAnimation;

    public void Initialize(int layers, PlayerAnimations startingAnimation, Animator animator, Action<int> DefaultAnimation)
    {
        layerLocked = new bool[layers];
        currentAnimation = new PlayerAnimations[layers];
        this.animator = animator;
        this.DefaultAnimation = DefaultAnimation;

        for (int i = 0; i < layers; i++)
        {
            layerLocked[i] = false;
            currentAnimation[i] = startingAnimation;
        }
    }

    public PlayerAnimations GetCurrentAnimation(int layer)
    {
        return currentAnimation[layer];
    }

    public void SetLocked(bool lockLayer, int layer)
    {
        layerLocked[layer] = lockLayer;
    }

    public void Play(PlayerAnimations animation, int layer, bool lockLayer, bool bypassLock, float crossfade = 0.2f)
    {
        if (animation == PlayerAnimations.NONE)
        {
            DefaultAnimation(layer);
            return;
        }

        if (layerLocked[layer] && !bypassLock) return;
        layerLocked[layer] = lockLayer;

        if (bypassLock)
            foreach (var item in animator.GetBehaviours<OnExitPlayer>())
                if (item.layerIndex == layer)
                    item.cancel = true;

        if (currentAnimation[layer] == animation) return;

        currentAnimation[layer] = animation;
        animator.CrossFade(animations[(int)currentAnimation[layer]], crossfade, layer);
    }
}

public enum PlayerAnimations
{
    NONE,
    IDLE,
    WALK,
    ATTACK,
    DEATH
}
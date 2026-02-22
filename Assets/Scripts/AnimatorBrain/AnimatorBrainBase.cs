using UnityEngine;
using System;

public class AnimatorBrainBase : MonoBehaviour
{
    // INHERITED MUST-MUST HAVE THESE ANIMATIONS
    public enum Animations
    {
        NONE = 0,
        IDLE = 1,
        WALK = 2,
        ATTACK = 3,
        DEATH = 4
    }
    protected static int[] animations = { 0, 0, 0, 0, 0 };
    protected Animator animator;
    protected int[] currentAnimation;
    protected bool[] layerLocked;
    protected Action<int> DefaultAnimation;

    public void Initialize(int layers, int startingAnimation, Animator animator, Action<int> DefaultAnimation)
    {
        layerLocked = new bool[layers];
        currentAnimation = new int[layers];
        this.animator = animator;
        this.DefaultAnimation = DefaultAnimation;

        for (int i = 0; i < layers; i++)
        {
            layerLocked[i] = false;
            currentAnimation[i] = startingAnimation;
        }
    }

    public int GetCurrentAnimation(int layer)
    {
        return currentAnimation[layer];
    }

    public void SetLocked(bool lockLayer, int layer)
    {
        layerLocked[layer] = lockLayer;
    }

    public void Play(int animation, int layer, bool lockLayer, bool bypassLock, float crossfade = 0.2f)
    {
        if (animation == 0)
        {
            DefaultAnimation(layer);
            return;
        }

        if (layerLocked[layer] && !bypassLock) return;
        layerLocked[layer] = lockLayer;

        if (bypassLock)
            HandleBypassLock(layer);

        if (currentAnimation[layer] == animation) return;

        currentAnimation[layer] = animation;
        animator.CrossFade(animations[(int)currentAnimation[layer]], crossfade, layer);
    }

    protected virtual void HandleBypassLock(int layer)
    {
    }
}

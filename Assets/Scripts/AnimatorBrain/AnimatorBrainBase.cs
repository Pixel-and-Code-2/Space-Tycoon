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
        DEATH = 4,
        DAMAGE = 5,
    }
    protected int[] animations = { 0, 0, 0, 0, 0 };
    protected int[] subAnimations = { 0, 0, 0, 0, 0 };
    protected bool isSubEnables = false;
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
            if (isSubEnables && layer == 0)
                DefaultAnimation(layer + 1);
            return;
        }

        if (layerLocked[layer] && !bypassLock) return;
        layerLocked[layer] = lockLayer;

        if (bypassLock)
            HandleBypassLock(layer);

        if (currentAnimation[layer] == animation) return;

        currentAnimation[layer] = animation;
        animator.CrossFade(animations[(int)currentAnimation[layer]], crossfade, layer);
        if (isSubEnables && layer == 0)
        {
            currentAnimation[layer + 1] = animation;
            animator.CrossFade(subAnimations[currentAnimation[layer + 1]], crossfade, layer + 1);
        }
    }

    public void InstaPlay(int animation, int layer, bool lockLayer = true, bool bypassLock = true)
    {
        if (animation == 0)
        {
            DefaultAnimation(layer);
            if (isSubEnables && layer == 0)
                DefaultAnimation(layer + 1);
            return;
        }

        if (layerLocked[layer] && !bypassLock) return;
        layerLocked[layer] = lockLayer;

        if (bypassLock)
            HandleBypassLock(layer);

        currentAnimation[layer] = animation;
        animator.Play(animations[animation], layer, 1f);
        if (isSubEnables && layer == 0)
        {
            currentAnimation[layer + 1] = animation;
            animator.Play(subAnimations[animation], layer + 1, 1f);
        }
        animator.Update(0f);
    }

    protected virtual void HandleBypassLock(int layer)
    {
    }
}

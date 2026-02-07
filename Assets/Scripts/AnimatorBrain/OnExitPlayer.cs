using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnExitPlayer : StateMachineBehaviour
{
    [SerializeField] private PlayerAnimations animation;
    [SerializeField] private bool lockLayer;
    [SerializeField] private float crossfade = 0.2f;
    [HideInInspector] public bool cancel = false;
    [HideInInspector] public int layerIndex = -1;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        this.layerIndex = layerIndex;
        cancel = false;
        PawnController.Instance.StartCoroutine(Wait());

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(stateInfo.length - crossfade);

            if (cancel) yield break;

            AnimatorBrainPlayer target = animator.GetComponent<AnimatorBrainPlayer>();
            target.SetLocked(false, layerIndex);
            target.Play(animation, layerIndex, lockLayer, false, crossfade);
        }
    }
}
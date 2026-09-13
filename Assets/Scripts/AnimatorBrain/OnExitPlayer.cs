using System.Collections;
using UnityEngine;

public class OnExitPlayer : StateMachineBehaviour
{
    [SerializeField] private int animation;
    [SerializeField] private bool lockLayer;
    [SerializeField] private float crossfade = 0.2f;
    [HideInInspector] public bool cancel = false;
    [HideInInspector] public int layerIndex = -1;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        this.layerIndex = layerIndex;
        cancel = false;
        if (PawnController.Instance == null) return;
        PawnController.Instance.StartCoroutine(Wait(animator, stateInfo, layerIndex));
    }

    IEnumerator Wait(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float wait = Mathf.Max(0.05f, stateInfo.length - crossfade);
        yield return new WaitForSeconds(wait);
        if (cancel || animator == null) yield break;

        AnimatorBrainPlayer target = animator.GetComponent<AnimatorBrainPlayer>();
        if (target == null) yield break;
        target.SetLocked(false, layerIndex);
        target.ForcePlay(animation, layerIndex, lockLayer, crossfade);
    }
}

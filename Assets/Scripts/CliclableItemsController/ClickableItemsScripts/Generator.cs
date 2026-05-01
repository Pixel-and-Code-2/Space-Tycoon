using UnityEngine;

public class Generator : IScriptForClickable
{

    [SerializeField]
    private float endAfterProgress = 50f;
    public override float OnProgress(float newProgress)
    {
        newProgress = base.OnProgress(newProgress);
        if (newProgress >= endAfterProgress)
        {
            return 100f;
        }
        return newProgress;
    }
}
using UnityEngine;

public class MovableLookTarget : ILookTarget
{
    public override Transform GetTransform()
    {
        return transform;
    }
}
using UnityEngine;

public abstract class ILookTarget : MonoBehaviour
{
    public abstract Transform GetTransform();
    public virtual bool isMovable => true;
}
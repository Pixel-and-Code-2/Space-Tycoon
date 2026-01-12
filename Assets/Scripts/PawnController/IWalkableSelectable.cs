using UnityEngine;

interface IWalkableSelectable : ISelectable
{
    void OnMove(Vector3 position);

    Vector3[] GetPathPointsTo(Vector3 position);

    float GetAvailableDistance();
}
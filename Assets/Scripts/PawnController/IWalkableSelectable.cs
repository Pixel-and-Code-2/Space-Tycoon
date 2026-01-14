using UnityEngine;

interface IWalkableSelectable : ISelectable
{
    void OnMove(Vector3 position);

    (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position);
}
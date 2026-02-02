using UnityEngine;

interface IControlableSelectable : ISelectable
{
    void OnMove(Vector3 position);
    void OnShoot(Vector3 position);

    (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position);

    bool IsMoving();
}
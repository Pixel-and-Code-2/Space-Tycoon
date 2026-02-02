using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PathDrawer))]
public class PawnBrain : PawnNavMesh, IControlableSelectable
{
    private PathDrawer pathDrawer;

    protected override void Awake()
    {
        base.Awake();
        pathDrawer = GetComponent<PathDrawer>();
    }

    protected override void Update()
    {
        base.Update();
        if (isMoving)
        {
            if (!pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(true);
                pathDrawer.SetPathPoints(GetPathPointsTo(targetPosition).pointsAvailable, null);
            }
            else
            {
                // we can update route runtime, but it's epensive
                // pathDrawer.SetPathPoints(GetPathPointsTo(targetPosition).pointsAvailable, null);
            }
        }
        else
        {
            if (pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(false);
            }
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnMove(Vector3 position)
    {
        TravelToPosition(position);
    }

    public void OnShoot(Vector3 position)
    {
        Debug.Log("OnShoot: " + position);
        transform.LookAt(position);
        // transform.position += Vector3.up * 10f;
        transform.position += transform.forward * 0.2f;
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.rigidbody) return;
        Vector3 dir = -transform.position + other.transform.position;
        dir.y = initialPlayerData.verticalPushOverride;
        other.rigidbody.AddForce(dir * initialPlayerData.obstaclePushForce, ForceMode.Impulse);
    }
}
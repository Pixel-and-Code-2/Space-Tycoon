using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PointMagnetiser : MonoBehaviour
{
    [SerializeField]
    private bool drawGizmos = false;
    [SerializeField]
    private float movementSpeedTrashold = 0.01f;
    [SerializeField]
    private float magnetisationTrashold = 0.01f;
    private const float forceRoof = 10f;

    private bool isMagnetising = false;
    private float noMoveStartTime = 0f;
    [SerializeField]
    private Vector3 magnetisationPosition = Vector3.zero;

    [SerializeField]
    private float oneUnitMultiplier = 5f;
    [SerializeField]
    private AnimationCurve speedDistanceRelation = AnimationCurve.EaseInOut(-1f, -1f, 1f, 1f);
    private new Rigidbody rigidbody;

    private Action magnetiseCallback = null;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
    }

    private Vector3 forceCache = Vector3.zero;
    void Update()
    {
        if (isMagnetising)
        {
            Vector3 distance = (magnetisationPosition - transform.position) / oneUnitMultiplier;
            Vector3 speed = rigidbody.linearVelocity;
            Vector3 desiredSpeed = new Vector3(
                speedDistanceRelation.Evaluate(distance.x),
                speedDistanceRelation.Evaluate(distance.y),
                speedDistanceRelation.Evaluate(distance.z)
            );
            Vector3 force = desiredSpeed * oneUnitMultiplier - speed;
            if (force.magnitude > forceRoof)
            {
                force = force.normalized * forceRoof;
            }
            forceCache = force;
            rigidbody.AddForce(force, ForceMode.Force);


            if (noMoveStartTime <= 0f && speed.magnitude < movementSpeedTrashold)
            {
                noMoveStartTime = Time.time;
            }

            if (noMoveStartTime > 0f && speed.magnitude > movementSpeedTrashold)
            {
                noMoveStartTime = 0f;
            }
            if (noMoveStartTime > 0f && Time.time - noMoveStartTime > 1f)
            {
                transform.position = magnetisationPosition;
                noMoveStartTime = 0f;
            }

            if (distance.magnitude < magnetisationTrashold && speed.magnitude < movementSpeedTrashold)
            {
                isMagnetising = false;
                magnetiseCallback?.Invoke();
                // this shoud be tough
                magnetiseCallback = null;
                rigidbody.linearVelocity = Vector3.zero;
                transform.position = magnetisationPosition;
            }
        }
        else if (rigidbody.linearVelocity.magnitude != 0f)
        {
            rigidbody.linearVelocity = Vector3.zero;
        }
    }

    void OnValidate()
    {
        if ((magnetisationPosition - transform.position).magnitude >= magnetisationTrashold && magnetisationPosition != Vector3.zero)
        {
            isMagnetising = true;
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, magnetisationPosition);
        Gizmos.DrawSphere(magnetisationPosition, 0.1f);
        Gizmos.color = isMagnetising ? Color.blue : Color.purple;
        Gizmos.DrawLine(transform.position, transform.position + forceCache.normalized);
        Gizmos.DrawSphere(transform.position + forceCache.normalized, 0.1f);
    }

    public void MagnetiseToPosition(Vector3 position, Action callback = null)
    {
        magnetisationPosition = position;
        isMagnetising = true;
        magnetiseCallback = callback;
    }
}
using System;
using UnityEngine;



public class GateWay : MonoBehaviour
{
    static private Vector3 gateWayScale = new Vector3(3, 3, 0.1f);
    static private bool drawGizmos = false;
    public const int NO_GATEWAY_ID = -1;

    [SerializeField]
    private Vector3 exitDirection;

    [SerializeField]
    private Vector3 positionOffset;

    [SerializeField, HideInInspector]
    public int id = GateWay.NO_GATEWAY_ID;
    [SerializeField, HideInInspector]
    public int connectedGatewayId = GateWay.NO_GATEWAY_ID;

    [SerializeField, HideInInspector]
    private bool isOccupied = false;

    public Vector3 GetPosition()
    {
        Vector3 modulePos = transform.position;
        Vector3 globalPositionOffset = transform.TransformDirection(positionOffset);
        Vector3 position = modulePos + globalPositionOffset;
        return position;
    }

    public Quaternion GetRotation()
    {
        Vector3 globalExitDirection = transform.TransformDirection(exitDirection);
        Vector3 upVector = transform.TransformDirection(Vector3.up);
        Quaternion rotation = Quaternion.LookRotation(globalExitDirection, upVector);
        return rotation;
    }

    public Vector3 GetExitDirection()
    {
        Vector3 globalExitDirection = transform.TransformDirection(exitDirection);
        return globalExitDirection;
    }

    public void LinkToAnotherGateWay(GateWay otherGateWay) // transfers this GameObject to fit to otherGateWay
    {
        otherGateWay.isOccupied = true;
        isOccupied = true;
        otherGateWay.connectedGatewayId = id;
        connectedGatewayId = otherGateWay.id;

        Vector3 upVector = otherGateWay.GetRotation() * Vector3.up;
        Vector3 otherExitDirection = otherGateWay.GetExitDirection().normalized;
        Quaternion myNewGateWayRot = Quaternion.LookRotation(-otherExitDirection, upVector);
        Quaternion currentWorldRot = GetRotation();
        Quaternion deltaRot = myNewGateWayRot * Quaternion.Inverse(currentWorldRot);
        transform.rotation = deltaRot * transform.rotation;

        Vector3 myNewGateWayPos = otherGateWay.GetPosition() + otherGateWay.GetExitDirection() * gateWayScale.x * 0.5f;
        Vector3 new_pos = myNewGateWayPos - transform.TransformDirection(positionOffset);
        transform.position = new_pos;

        Debug.Log("Linking GateWay " + id + " to " + otherGateWay.id);
    }

    void OnDrawGizmos()
    {

        if (drawGizmos)
        {
            Gizmos.color = !isOccupied ? Color.red : Color.green;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(GetPosition(), GetRotation(), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, gateWayScale);
            Gizmos.matrix = oldMatrix;

            Vector3 start = GetPosition();
            Vector3 dir = transform.TransformDirection(exitDirection).normalized;
            float length = gateWayScale.x * 0.5f;
            Vector3 end = start + dir * length;
            Gizmos.DrawLine(start, end);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            Vector3 labelPos = GetPosition();
            UnityEditor.Handles.Label(labelPos, id.ToString());
#endif
        }
    }
}

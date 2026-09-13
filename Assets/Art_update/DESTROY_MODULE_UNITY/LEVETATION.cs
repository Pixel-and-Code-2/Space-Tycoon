using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������ ������� �������� ������� �� ���� ���� � ������ ���������� ��������� ������� ���������.
/// </summary>
public class RandomRotation : MonoBehaviour
{
    [Header("��������� ��������")]
    [Tooltip("������������ ������� �������� (�������� � �������) �� ������ ���")]
    public float maxAngularSpeed = 90f;

    [Tooltip("�������� (� ��������) ����� ������ ������� ��������")]
    public float changeInterval = 3f;

    [Tooltip("����� �������� �������� � ����� �������� (��� ������, ��� �������)")]
    public float smoothTime = 1f;

    // ������ ��� ������� ��������� �������
    private List<ChildData> children = new List<ChildData>();

    private void Start()
    {
        // �������� ��� ������ �������� ����������
        foreach (Transform child in transform)
        {
            var data = new ChildData
            {
                transform = child,
                // ��������� ��������� ��������
                currentVelocity = Random.insideUnitSphere * maxAngularSpeed,
                targetVelocity = Vector3.zero,
                velocityRef = Vector3.zero,
                timer = Random.Range(0f, changeInterval) // ��������� ��������, ����� �� ������������������
            };
            data.targetVelocity = data.currentVelocity; // ����� ����� ����
            children.Add(data);
        }
    }

    private void Update()
    {
        foreach (var data in children)
        {
            if (data.transform == null) continue; // ������ �� ��������

            // ��������� ������ � ��� ������������� ������ ����
            data.timer += Time.deltaTime;
            if (data.timer >= changeInterval)
            {
                data.timer = 0f;
                // ����� ��������� �������� � �������� ����� ������� maxAngularSpeed
                data.targetVelocity = Random.insideUnitSphere * maxAngularSpeed;
            }

            // ������ �������� ������� �������� � ������� �� ������ ���
            data.currentVelocity.x = Mathf.SmoothDamp(
                data.currentVelocity.x,
                data.targetVelocity.x,
                ref data.velocityRef.x,
                smoothTime
            );
            data.currentVelocity.y = Mathf.SmoothDamp(
                data.currentVelocity.y,
                data.targetVelocity.y,
                ref data.velocityRef.y,
                smoothTime
            );
            data.currentVelocity.z = Mathf.SmoothDamp(
                data.currentVelocity.z,
                data.targetVelocity.z,
                ref data.velocityRef.z,
                smoothTime
            );

            // ������������ ������ ������ ��� ��������� ����
            Vector3 delta = data.currentVelocity * Time.deltaTime;
            if (float.IsNaN(delta.x) || float.IsNaN(delta.y) || float.IsNaN(delta.z)
                || float.IsInfinity(delta.x) || float.IsInfinity(delta.y) || float.IsInfinity(delta.z))
            {
                data.currentVelocity = Vector3.zero;
                data.velocityRef = Vector3.zero;
                continue;
            }
            data.transform.Rotate(delta, Space.Self);
        }
    }

    /// <summary>
    /// ��������������� ����� ��� �������� ��������� ������� ������.
    /// </summary>
    [System.Serializable]
    private class ChildData
    {
        public Transform transform;
        public Vector3 currentVelocity; // ������� ������� ��������
        public Vector3 targetVelocity;  // ������� �������� (� ������� ���������)
        public Vector3 velocityRef;     // ��������������� ���������� ��� SmoothDamp
        public float timer;             // ������ �� ��������� ����� ����
    }
}
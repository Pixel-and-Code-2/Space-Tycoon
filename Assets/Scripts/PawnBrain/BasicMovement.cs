using UnityEngine;
using System;
using System.Collections.Generic;

public class BasicMovement : MonoBehaviour
{
    [SerializeField]
    private float speed = 10f;

    private Vector3 targetPosition = Vector3.zero;
    private bool isMoving = false;

    void Update()
    {
        if (isMoving)
        {
            transform.Translate((transform.position - targetPosition).normalized * speed * Time.deltaTime);
        }
    }

    public void LinearMoveToPosition(Vector3 position, Action callback = null)
    {
        targetPosition = position;
        isMoving = true;
        callback?.Invoke();
    }

    public void TeleportToPosition(Vector3 position, Action callback = null)
    {
        transform.position = position;
        callback?.Invoke();
    }

    public IEnumerator<WaitForSeconds> DelayCallback(float delay, Action callback = null)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}
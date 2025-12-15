using UnityEngine;
using System;
using System.Collections.Generic;

public class BasicMovement : MonoBehaviour
{
    [SerializeField]
    private float speed = 10f;

    private Vector3 targetPosition = Vector3.zero;
    private bool isMoving = false;
    private Action callBackCache = null;
    [SerializeField]
    private float distanceTrashold = 0.01f;

    void Update()
    {
        if (isMoving)
        {
            Vector3 diff = targetPosition - transform.position;
            transform.Translate(diff.normalized * speed * Time.deltaTime);
            if (diff.normalized.magnitude * speed * Time.deltaTime > diff.magnitude || diff.magnitude < distanceTrashold)
            {
                transform.position = targetPosition;
                isMoving = false;
                callBackCache?.Invoke();
            }
        }
    }

    public void LinearMoveToPosition(Vector3 position, Action callback = null)
    {
        targetPosition = position;
        isMoving = true;
        callBackCache = callback;
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
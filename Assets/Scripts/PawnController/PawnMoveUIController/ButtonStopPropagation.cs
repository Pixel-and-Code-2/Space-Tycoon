using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Button))]
public class ButtonStopPropagation : MonoBehaviour, IPointerDownHandler
{
    public static Action OnUIClickHandled;
    public void OnPointerDown(PointerEventData eventData)
    {
        // Debug.Log("OnPointerDown: " + eventData.pointerPress.name);
        OnUIClickHandled?.Invoke();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // <-- ÎÁßÇÀÒÅËÜÍÎ äîáàâëÿåì ýòî

public class RotateToMouse : MonoBehaviour
{

    public Camera cam;
    public float maximumLenght;

    private Ray rayMouse;
    private Vector3 pos;
    private Vector3 direction;
    private Quaternion rotation;

    void Update()
    {
        // Ïðîâåðÿåì, ÷òî êàìåðà çàäàíà è ìûøü ïîäêëþ÷åíà
        if (cam != null && Mouse.current != null)
        {
            RaycastHit hit;

            // ÏÎËÓ×ÀÅÌ ÏÎÇÈÖÈÞ ÌÛØÈ ×ÅÐÅÇ ÍÎÂÓÞ ÑÈÑÒÅÌÓ
            Vector2 mousePos = Mouse.current.position.ReadValue();

            rayMouse = cam.ScreenPointToRay(mousePos);
            if (Physics.Raycast(rayMouse.origin, rayMouse.direction, out hit, maximumLenght))
            {
                RotateToMouseDirection(gameObject, hit.point);
            }
            else
            {
                pos = rayMouse.GetPoint(maximumLenght);
                RotateToMouseDirection(gameObject, pos);
            }
        }
        else
        {
            // Åñëè êàìåðà íå çàäàíà
            // Debug.Log ("No Camera or Mouse"); 
        }
    }

    void RotateToMouseDirection(GameObject obj, Vector3 destination)
    {
        direction = destination - obj.transform.position;
        rotation = Quaternion.LookRotation(direction);
        obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1);
    }

    public Quaternion GetRotation()
    {
        return rotation;
    }
}

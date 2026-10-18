using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // <-- Добавлено пространство имен

public class SpawnProjectiles : MonoBehaviour
{
    public GameObject firePoint;
    public List<GameObject> vfx = new List<GameObject>();
    public RotateToMouse rotateToMouse;

    private GameObject effectToSpawn;

    void Start()
    {
        if (vfx.Count > 0) effectToSpawn = vfx[0];
    }

    void Update()
    {
        // Проверка нажатия левой кнопки мыши через новую систему
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnVFX();
        }
    }

    void SpawnVFX()
    {
        if (firePoint != null && effectToSpawn != null)
        {
            Quaternion spawnRotation = (rotateToMouse != null) ? rotateToMouse.GetRotation() : firePoint.transform.rotation;
            GameObject newVfx = Instantiate(effectToSpawn, firePoint.transform.position, spawnRotation);
            Destroy(newVfx, 5f);
        }
        else
        {
            Debug.Log("No Fire Point or Effect");
        }
    }
}
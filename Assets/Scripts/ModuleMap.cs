using System.Collections.Generic;
using UnityEngine;

public class ModuleMap : MonoBehaviour
{
    [SerializeField]
    private GameObject initialModulePrefab = null;

    [SerializeField]
    private ModuleDataList moduleList = new ModuleDataList();


    void Start()
    {
        moduleList.Clear();
        moduleList.spawnContext = this.gameObject;
        initialModulePrefab = Instantiate(initialModulePrefab, transform);
        moduleList.Add(new ModuleData
        {
            position = Vector3.zero,
            module = initialModulePrefab
        });
    }

    void OnValidate()
    {
        if (initialModulePrefab != null && this.transform.childCount == 0) Start();
        moduleList.OnValidate();
    }

}

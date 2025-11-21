using System.Collections.Generic;
using UnityEngine;

public class ModuleMap : MonoBehaviour
{
    [SerializeField]
    private GameObject initialModulePrefab;

    [SerializeField]
    private ModuleDataList moduleList = new ModuleDataList();


    void Start()
    {
        initialModulePrefab = Instantiate(initialModulePrefab);
        moduleList.Add(new ModuleData
        {
            position = transform.position,
            module = initialModulePrefab
        });
    }

    void OnValidate()
    {
        moduleList.OnValidate();
    }

}

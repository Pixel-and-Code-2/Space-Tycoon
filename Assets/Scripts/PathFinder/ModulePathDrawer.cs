// this class does nothing yet, no tests were done


using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ModuleMap))]
public class ModulePathDrawer : MonoBehaviour
{
    [SerializeField]
    private bool drawGizmos = true;
    [SerializeField]
    private Color pathColor = Color.cyan;

    private ModuleMap moduleMap;
    private List<Vector3> lastFoundPath = new List<Vector3>();

    private void Start()
    {
        moduleMap = GetComponent<ModuleMap>();
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        var path = FindPath(startPos, targetPos);
        if (path != null)
        {
            lastFoundPath = path;
        }
        return path;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || lastFoundPath == null || lastFoundPath.Count < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < lastFoundPath.Count - 1; i++)
        {
            Gizmos.DrawLine(lastFoundPath[i], lastFoundPath[i + 1]);
            Gizmos.DrawSphere(lastFoundPath[i], 0.1f);
        }
        Gizmos.DrawSphere(lastFoundPath[lastFoundPath.Count - 1], 0.1f);
    }
}

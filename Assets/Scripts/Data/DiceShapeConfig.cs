using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Space-Tycoon/Dice Shape Config", fileName = "DiceShapeConfig")]
public class DiceShapeConfig : ScriptableObject
{
    [System.Serializable]
    public class StickEntry
    {
        public Vector3 localNormal = Vector3.up;
        public Vector3 localEulerExtra = Vector3.zero;
        public int displayValue = 1;
        public bool draw = true;
        public Color color = Color.cyan;
    }

    public DiceFaceMap diePrefab;
    public Vector3 globalNormalsEuler = Vector3.zero;
    public float stickLength = 0.55f;
    public float labelScale = 0.08f;
    public List<StickEntry> sticks = new List<StickEntry>();

    public Vector3 ResolveLocalNormal(StickEntry stick)
    {
        if (stick == null) return Vector3.up;
        Quaternion q = Quaternion.Euler(globalNormalsEuler) * Quaternion.Euler(stick.localEulerExtra);
        Vector3 n = stick.localNormal;
        if (n.sqrMagnitude < 0.0001f) n = Vector3.up;
        return (q * n.normalized).normalized;
    }

    public List<DiceFaceMap.Face> BuildFaces()
    {
        List<DiceFaceMap.Face> faces = new List<DiceFaceMap.Face>();
        if (sticks == null) return faces;
        for (int i = 0; i < sticks.Count; i++)
        {
            StickEntry s = sticks[i];
            if (s == null) continue;
            faces.Add(new DiceFaceMap.Face
            {
                localNormal = ResolveLocalNormal(s),
                value = s.displayValue
            });
        }
        return faces;
    }

    public void ApplyTo(DiceFaceMap map)
    {
        if (map == null) return;
        map.ApplyFaces(BuildFaces());
    }
}

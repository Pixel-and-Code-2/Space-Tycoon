using UnityEngine;
using System.Collections.Generic;

public class DiceFaceMap : MonoBehaviour
{
    [System.Serializable]
    public struct Face
    {
        public Vector3 localNormal;
        public int value;
    }

    public List<Face> faces = new List<Face>();
    [Range(0.01f, 0.5f)]
    public float edgeThreshold = 0.12f;
    [Range(0.01f, 0.35f)]
    public float cornerThreshold = 0.2f;
    [Range(0.85f, 1f)]
    public float superCritDot = 0.995f;

    public enum ReadKind
    {
        Normal,
        Edge,
        Corner,
        SuperCrit
    }

    public struct ReadResult
    {
        public int value;
        public ReadKind kind;
        public int secondaryValue;
    }

    public ReadResult ReadUp(bool sumOnEdge = true)
    {
        ReadResult r = new ReadResult { value = 1, kind = ReadKind.Normal };
        if (faces == null || faces.Count == 0) return r;

        Vector3 up = Vector3.up;
        float best = -2f;
        float second = -2f;
        int bestIdx = 0;
        int secondIdx = 0;
        for (int i = 0; i < faces.Count; i++)
        {
            Vector3 n = transform.TransformDirection(faces[i].localNormal.normalized);
            float d = Vector3.Dot(n, up);
            if (d > best)
            {
                second = best;
                secondIdx = bestIdx;
                best = d;
                bestIdx = i;
            }
            else if (d > second)
            {
                second = d;
                secondIdx = i;
            }
        }

        r.value = faces[bestIdx].value;
        r.secondaryValue = faces[secondIdx].value;
        if (best < cornerThreshold)
            r.kind = ReadKind.Corner;
        else if (best - second < edgeThreshold)
            r.kind = ReadKind.Edge;
        else if (best >= superCritDot)
            r.kind = ReadKind.SuperCrit;
        else
            r.kind = ReadKind.Normal;

        if (r.kind == ReadKind.Edge && sumOnEdge)
            r.value = faces[bestIdx].value + faces[secondIdx].value;
        return r;
    }

    public void ApplyFaces(List<Face> source)
    {
        faces = source != null ? new List<Face>(source) : new List<Face>();
    }
}

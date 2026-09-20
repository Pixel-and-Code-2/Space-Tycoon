using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Space-Tycoon/Dice Shape Config", fileName = "DiceShapeConfig")]
public class DiceShapeConfig : ScriptableObject
{
    [System.Serializable]
    public class StickEntry
    {
        [Tooltip("Local rotation like Transform: after this, +Y points out of the face")]
        public Vector3 eulerAngles = Vector3.zero;
        public int displayValue = 1;
        public bool draw = true;
        public Color color = Color.cyan;
        [Tooltip("OnValidate one-shot: aim +Y along SceneView look (toward viewer), then clears")]
        public bool aimAtCamera;
    }

    [Tooltip("Die sides this config represents (6, 10, 20, ...)")]
    public int sides = 10;
    public DiceFaceMap diePrefab;
    public float stickLength = 0.55f;
    public float labelScale = 0.08f;
    public List<StickEntry> sticks = new List<StickEntry>();

    public Vector3 ResolveLocalNormal(StickEntry stick)
    {
        if (stick == null) return Vector3.up;
        return (Quaternion.Euler(stick.eulerAngles) * Vector3.up).normalized;
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

#if UNITY_EDITOR
    void OnValidate()
    {
        if (sticks == null) return;
        Camera cam = null;
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null) cam = sv.camera;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        // Along view axis toward the viewer (not toward camera.position —
        // SceneView lens sits above the orbit pivot and looks "too high").
        Vector3 toViewer = -cam.transform.forward;
        if (toViewer.sqrMagnitude < 0.0001f) return;
        toViewer.Normalize();
        bool dirty = false;
        for (int i = 0; i < sticks.Count; i++)
        {
            StickEntry s = sticks[i];
            if (s == null || !s.aimAtCamera) continue;
            s.eulerAngles = Quaternion.FromToRotation(Vector3.up, toViewer).eulerAngles;
            s.aimAtCamera = false;
            dirty = true;
        }
        if (dirty)
            EditorUtility.SetDirty(this);
    }
#endif
}

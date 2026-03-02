using UnityEngine;

[System.Serializable]
public struct ExitCode
{
    private static float eps = 0.001f;
    public string message;
    public Color color;
    public float code;
    public bool IsEqual(float other)
    {
        return Mathf.Abs(code - other) < eps;
    }
}
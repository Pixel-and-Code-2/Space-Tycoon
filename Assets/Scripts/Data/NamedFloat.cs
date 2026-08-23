using UnityEngine;

[System.Serializable]
public class NamedFloat
{
    public NamedFloat(string name, float value)
    {
        this.name = name;
        this.value = value;
    }
    public string name;
    public float value;
}

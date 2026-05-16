using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public enum LightModeType
{
    NO_BATTLE,
    BATTLE,
    QUARANTINE
}

[System.Serializable]
public class LightMode
{
    public LightModeType lightModeType;
    public Color colorA = new(1f, 0.945f, 0.851f, 1f);
    public Color colorB = new(0.863f, 0.369f, 0.349f, 1f);
    [Range(0f, 10f)] public float duration = 1f;
    public AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));
}
public class LightsController : MonoBehaviour
{
    private Light[] lights;
    [SerializeField]
    private List<LightMode> lightModes;
    private LightMode currentLightMode;

    private float t = 0f;
    void Awake()
    {
        lights = GetComponentsInChildren<Light>();
        currentLightMode = lightModes[0];
    }
    void Start()
    {
        TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        TurnManager.Instance.OnTriggerZoneEnter += OnTriggerZoneEnter;
    }
    void Update()
    {
        t += Time.deltaTime;
        float value = currentLightMode.curve.Evaluate((t % currentLightMode.duration) / currentLightMode.duration);
        foreach (Light light in lights)
        {
            light.color = Color.Lerp(currentLightMode.colorA, currentLightMode.colorB, value);
        }
    }
    void OnValidate()
    {
        HashSet<LightModeType> unusedModes = new HashSet<LightModeType>();
        foreach (LightModeType lightModeType in System.Enum.GetValues(typeof(LightModeType)))
        {
            unusedModes.Add(lightModeType);
        }
        for (int i = 0; i < lightModes.Count; i++)
        {
            if (unusedModes.Contains(lightModes[i].lightModeType))
            {
                unusedModes.Remove(lightModes[i].lightModeType);
            }
            else
            {
                if (unusedModes.Count > 0)
                {
                    lightModes[i].lightModeType = unusedModes.First();
                    unusedModes.Remove(lightModes[i].lightModeType);
                }
                else
                {
                    lightModes.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    void ChangeLightMode(LightModeType lightModeType)
    {
        currentLightMode = lightModes.FirstOrDefault(mode => mode.lightModeType == lightModeType);
        t = 0f;
    }
    void OnTriggerZoneExit()
    {
        ChangeLightMode(LightModeType.NO_BATTLE);
    }
    void OnTriggerZoneEnter()
    {
        if (TurnManager.Instance.IsQuarantine)
        {
            Debug.Log("Quarantine");
            ChangeLightMode(LightModeType.QUARANTINE);
        }
        else
        {
            Debug.Log("Battle");
            ChangeLightMode(LightModeType.BATTLE);
        }
    }
}
using UnityEngine;
using System.Collections;
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
    public static LightsController Instance { get; private set; }

    private Light[] lights;
    private float[] baseIntensities;
    [SerializeField]
    private List<LightMode> lightModes;
    private LightMode currentLightMode;
    private bool blackout;
    private float intensityScale = 1f;

    private float t = 0f;
    void Awake()
    {
        Instance = this;
        lights = GetComponentsInChildren<Light>();
        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseIntensities[i] = lights[i].intensity;
        currentLightMode = lightModes[0];
    }
    void Start()
    {
        TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        TurnManager.Instance.OnTriggerZoneEnter += OnTriggerZoneEnter;
        SaveHub.Instance.OnLoad += OnLoadData;
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    void OnLoadData(LoadedData data)
    {
        if (data.GetData("IsStepByStep", HandleInittingGlobalVars.UNIQUE_ID, false))
        {
            if (TurnManager.Instance.IsQuarantine)
                ChangeLightMode(LightModeType.QUARANTINE);
            else
                ChangeLightMode(LightModeType.BATTLE);
        }
        else
        {
            ChangeLightMode(LightModeType.NO_BATTLE);
        }
        SetBlackout(false);
    }
    void Update()
    {
        t += Time.deltaTime;
        float value = currentLightMode.curve.Evaluate((t % currentLightMode.duration) / currentLightMode.duration);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].color = Color.Lerp(currentLightMode.colorA, currentLightMode.colorB, value);
            lights[i].intensity = blackout ? 0f : baseIntensities[i] * intensityScale;
        }
    }
    void OnValidate()
    {
        HashSet<LightModeType> unusedModes = new HashSet<LightModeType>();
        foreach (LightModeType lightModeType in System.Enum.GetValues(typeof(LightModeType)))
            unusedModes.Add(lightModeType);
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

    public void SetBlackout(bool enabled)
    {
        blackout = enabled;
        intensityScale = enabled ? 0f : 1f;
        ApplyIntensityNow();
    }

    public IEnumerator RestoreLights(float duration)
    {
        blackout = false;
        if (duration <= 0.01f)
        {
            intensityScale = 1f;
            ApplyIntensityNow();
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            intensityScale = Mathf.Clamp01(elapsed / duration);
            ApplyIntensityNow();
            yield return null;
        }
        intensityScale = 1f;
        ApplyIntensityNow();
    }

    void ApplyIntensityNow()
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].intensity = blackout ? 0f : baseIntensities[i] * intensityScale;
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
            ChangeLightMode(LightModeType.QUARANTINE);
        else
            ChangeLightMode(LightModeType.BATTLE);
    }
}

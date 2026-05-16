using UnityEngine;

public class SMOOTH_COLOR : MonoBehaviour
{
    public Color colorA = new (1f, 0.945f, 0.851f, 1f);
    public Color colorB = new (0.863f, 0.369f, 0.349f, 1f);
    public float duration = 1f;

    private Light lightComponent;
    private bool useColorA = true;
    private float t = 0f;
    void Start()
    {
        lightComponent = GetComponent<Light>();
        if (lightComponent != null)
            lightComponent.color = colorA;
        else
            Debug.LogError("Нет света на объекте");
    }

    void Update()
    {
        if (lightComponent == null) return;

        t += Time.deltaTime / duration;
        Color targetColor = useColorA ? colorB : colorA;
        lightComponent.color = Color.Lerp(useColorA ? colorA : colorB, targetColor, t);

        if (t >= 1f)
        {
            t = 0f;
            useColorA = !useColorA;
        }
    }
}

using UnityEngine;

public class PawnStatusVisualizer : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Visuals")]
    [SerializeField] private Renderer objectRenderer;
    private GlobalSettingsAssets settings => HandleInittingGlobalVars.globalSettingsAssets;

    private PawnBrain pawnBrain;
    private Material statusMaterial;
    private bool hasBaseColor;
    private bool hasShaderColor;
    private Color lastAppliedColor;
    private IControlableSelectable lastSelected;
    private bool hasAppliedOnce;

    void Awake()
    {
        if (objectRenderer == null) objectRenderer = GetComponent<Renderer>();

        pawnBrain = GetComponentInParent<PawnBrain>();

        if (objectRenderer == null) return;

        statusMaterial = objectRenderer.material;
        hasBaseColor = statusMaterial.HasProperty(BaseColorId);
        hasShaderColor = statusMaterial.HasProperty(ColorId);
    }

    void Start()
    {
        UpdateStatusColor();
    }

    void Update()
    {
        UpdateStatusColor();
    }

    private void UpdateStatusColor()
    {
        if (statusMaterial == null || pawnBrain == null || PawnController.Instance == null || settings == null)
            return;

        IControlableSelectable current = PawnController.Instance.currentSelectedPawn;
        if (hasAppliedOnce && current == lastSelected)
            return;

        Color targetColor = ResolveTargetColor(current);
        lastSelected = current;
        hasAppliedOnce = true;
        if (targetColor == lastAppliedColor)
            return;

        ApplyColor(targetColor);
    }

    private Color ResolveTargetColor(IControlableSelectable current)
    {
        SelectableType type = pawnBrain.GetSelectableType();
        if (type == SelectableType.Enemy)
            return settings.GetColorLink(current == pawnBrain ? settings.selectedColorEnemy : settings.enemyColor).color;
        if (type == SelectableType.Player)
            return settings.GetColorLink(current == pawnBrain ? settings.selectedColorAlly : settings.allyColor).color;
        return settings.GetColorLink(settings.deadColor).color;
    }

    private void ApplyColor(Color targetColor)
    {
        lastAppliedColor = targetColor;
        if (hasBaseColor)
            statusMaterial.SetColor(BaseColorId, targetColor);
        if (hasShaderColor)
            statusMaterial.SetColor(ColorId, targetColor);
        if (!hasBaseColor && !hasShaderColor)
            statusMaterial.color = targetColor;
    }
}

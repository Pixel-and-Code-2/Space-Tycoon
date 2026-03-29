using UnityEngine;

public class PawnStatusVisualizer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer objectRenderer;
    private GlobalSettingsAssets settings => HandleInittingGlobalVars.globalSettingsAssets;

    private PawnBrain pawnBrain;

    void Awake()
    {
        if (objectRenderer == null) objectRenderer = GetComponent<Renderer>();

        pawnBrain = GetComponentInParent<PawnBrain>();
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
        if (objectRenderer == null) return;

        Color targetColor = Color.white;
        IControlableSelectable current = PawnController.Instance.currentSelectedPawn;

        if (pawnBrain.GetSelectableType() == SelectableType.Enemy)
        {
            targetColor = current == pawnBrain ? settings.GetColorLink(settings.selectedColorEnemy).color : settings.GetColorLink(settings.enemyColor).color;
        }
        else if (pawnBrain.GetSelectableType() == SelectableType.Player)
        {
            targetColor = current == pawnBrain ? settings.GetColorLink(settings.selectedColorAlly).color : settings.GetColorLink(settings.allyColor).color;
        }
        else
        {
            targetColor = settings.GetColorLink(settings.deadColor).color;
        }



        if (objectRenderer.material.color != targetColor)
        {
            objectRenderer.material.color = targetColor;
        }
    }
}
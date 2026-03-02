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
            targetColor = current == pawnBrain ? settings.selectedColorEnemy : settings.enemyColor;
        }
        else if (pawnBrain.GetSelectableType() == SelectableType.Player)
        {
            targetColor = current == pawnBrain ? settings.selectedColorAlly : settings.allyColor;
        }
        else
        {
            targetColor = settings.deadColor;
        }



        if (objectRenderer.material.color != targetColor)
        {
            objectRenderer.material.color = targetColor;
        }
    }
}
using UnityEngine;

public class PawnStatusVisualizer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer objectRenderer;

    [Header("Colors")]
    [SerializeField] private Color allyColor = Color.green;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color deadColor = Color.gray;

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

        if (pawnBrain.GetSelectableType() == SelectableType.Enemy)
        {
            targetColor = enemyColor;
        }
        else if (pawnBrain.GetSelectableType() == SelectableType.Player)
        {
            targetColor = allyColor;
        }
        else
        {
            targetColor = deadColor;
        }


        if (objectRenderer.material.color != targetColor)
        {
            objectRenderer.material.color = targetColor;
        }
    }
}
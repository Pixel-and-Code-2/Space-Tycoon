using UnityEngine;

public class PawnStatusVisualizer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer objectRenderer;

    [Header("Colors")]
    [SerializeField] private Color allyColor = Color.green;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color deadColor = Color.gray;

    private SimpleEnemyAI enemyAI;
    private PawnBrain playerAI;

    void Awake()
    {
        if (objectRenderer == null) objectRenderer = GetComponent<Renderer>();

        enemyAI = GetComponentInParent<SimpleEnemyAI>();
        playerAI = GetComponentInParent<PawnBrain>();
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

        if (enemyAI != null)
        {
            if (enemyAI.GetSelectableType() == SelectableType.Dead)
            {
                targetColor = deadColor;
            }
            else
            {
                targetColor = enemyColor;
            }
        }
        else if (playerAI != null)
        {
            if (playerAI.GetSelectableType() == SelectableType.Dead)
            {
                targetColor = deadColor;
            }
            else
            {
                targetColor = allyColor;
            }
        }

        if (objectRenderer.material.color != targetColor)
        {
            objectRenderer.material.color = targetColor;
        }
    }
}
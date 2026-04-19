using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum PlayerIconState
{
    Disable,
    Selected,
    NotSelected
}

[RequireComponent(typeof(Button))]
public class PlayerIconController : MonoBehaviour
{
    [SerializeField]
    private GameObject DisableFG;
    [SerializeField]
    private GameObject Selected;
    [SerializeField]
    private GameObject NotSelected;
    [SerializeField]
    private TextMeshProUGUI playerHealingNumber;
    [SerializeField]
    private Button button;

    void OnEnable()
    {
        button = GetComponent<Button>();
    }

    public void UpdateState(PlayerIconState st)
    {
        switch (st)
        {
            case PlayerIconState.Disable:
                DisableFG.SetActive(true);
                Selected.SetActive(false);
                NotSelected.SetActive(true);
                button.interactable = false;
                break;
            case PlayerIconState.Selected:
                DisableFG.SetActive(false);
                Selected.SetActive(true);
                NotSelected.SetActive(false);
                button.interactable = true;
                break;
            case PlayerIconState.NotSelected:
                DisableFG.SetActive(false);
                Selected.SetActive(false);
                NotSelected.SetActive(true);
                button.interactable = true;
                break;
        }
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    public void UpdatePlayer(GameUI.PlayerGroup player)
    {
        float amountOfHealings = player.playerObject.GetDynamicParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY);
        float maxHealings = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
        playerHealingNumber.text = (maxHealings - amountOfHealings).ToString();
    }
}
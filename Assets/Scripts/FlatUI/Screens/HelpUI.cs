using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class HelpPage
{
    public List<GameObject> objects;
    public IconButtonStyleFiller currentPage;
}
public class HelpUI : IUILayer
{
    private int currentPage = 0;
    [SerializeField]
    private List<HelpPage> helpPages;
    void OnEnable()
    {
        UpdatePages();
    }

    public void OnClose()
    {
        UILayersController.Instance.GoBack();
    }

    public override void OnBackgroundClick()
    {
        OnClose();
    }

    public void OnNextPage()
    {
        currentPage++;
        if (currentPage >= helpPages.Count)
        {
            currentPage = 0;
        }
        UpdatePages();
    }

    public void OnPreviousPage()
    {
        currentPage--;
        if (currentPage < 0)
        {
            currentPage = helpPages.Count - 1;
        }
        UpdatePages();
    }

    private void UpdatePages()
    {
        for (int i = 0; i < helpPages.Count; i++)
        {
            helpPages[i].objects.ForEach(obj => obj.SetActive(i == currentPage));
            if (i == currentPage)
            {
                helpPages[i].currentPage.TurnOnButton();
            }
            else
            {
                helpPages[i].currentPage.TurnOffButton();
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);

    }

}
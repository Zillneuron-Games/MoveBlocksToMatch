using UnityEngine;

public class LoadingBar : MonoBehaviour
{
    [SerializeField]
    private GameObject loadingBarPanel;

    [SerializeField]
    private RectTransform fillRectTransform;

    public void SetFillPercentage(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);

        fillRectTransform.anchorMax = new Vector2(percentage, 1f);

        fillRectTransform.offsetMin = Vector2.zero;
        fillRectTransform.offsetMax = Vector2.zero;
    }

    public void Hide()
    {
        loadingBarPanel.SetActive(false);
    }
}

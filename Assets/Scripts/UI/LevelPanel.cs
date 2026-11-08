using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zillneuron.UILayout;

public class LevelPanel : AFragmentView
{
    private ILevelSelector levelSelector;

    [SerializeField]
    private int level;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private Image lockImage;

    [SerializeField]
    private Button levelButton;

    public void Construct(ILevelSelector levelSelector)
    {
        base.Construct();

        this.levelSelector = levelSelector;      
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        bool unlocked = levelSelector.Unlocked(level);

        levelButton.interactable = unlocked;
        levelText.gameObject.SetActive(unlocked);
        lockImage.gameObject.SetActive(!unlocked);
    }

    protected override void SetTexts()
    {
        base.SetTexts();

        levelText.text = level.ToString();
    }

    public void Select_ButtonHandler()
    {
        levelSelector.Select(level);
    }
}

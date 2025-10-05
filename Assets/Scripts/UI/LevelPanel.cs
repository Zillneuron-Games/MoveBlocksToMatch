using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class LevelPanel : AFragmentView
{
    private ILevelSelector levelSelector;

    [SerializeField]
    private int level;

    [SerializeField]
    private TMP_Text levelText;

    public void Construct(ILevelSelector levelSelector)
    {
        base.Construct();

        this.levelSelector = levelSelector;
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

using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class LevelsView : ACompleteView, ILevelSelector
{
    [SerializeField]
    private LevelPanel[] levelPanels;

    protected override void ConstructFragments()
    {
        base.ConstructFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Construct(this);
        }
    }

    protected override void LaunchFragments()
    {
        base.LaunchFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Launch();
        }
    }

    protected override void FinishFragments()
    {
        base.FinishFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Finish();
        }
    }

    protected override void DestructFragments()
    {
        base.DestructFragments();

        for (int i = 0; i < levelPanels.Length; i++)
        {
            levelPanels[i].Destruct();
        }
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        gameObject.SetActive(true);
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }

    public void Select(int level)
    {
        Debug.LogError($"Level : {level}");
        ChangeActivityToNextAndReplace<GameplayView>();
    }

    public void Back_ButtonHandler()
    {
        ChangeActivityToPrevious();
    }
}
